using GpsNotepad.GoogleService;
using GpsNotepad.Models;
using GpsNotepad.Services.Authorization;
using GpsNotepad.Services.Localization;
using GpsNotepad.Validation;
using GpsNotepad.Views;
using Newtonsoft.Json;
using OAuthNativeFlow;
using Plugin.FacebookClient;
using Prism.Commands;
using Prism.Navigation;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Windows.Input;
using Xamarin.Auth;
using Xamarin.Forms;

namespace GpsNotepad.ViewModels
{
    class CreateAccountSecondPageViewModel : BaseViewModel
    {
        Account account;
        [Obsolete]
        AccountStore store;
        private UserModel _userModel;
        private readonly IAuthorizationService _authorizationService;

        public CreateAccountSecondPageViewModel(IAuthorizationService authorizationService,
                                                ILocalizationService localizationService,
                                                INavigationService navigationService) : base(navigationService, localizationService)
        {
            _authorizationService = authorizationService;
            IsPassword = true;
            IsConfirmPassword = true;
            store = AccountStore.Create();
        }

        #region --- Public Properties ---

        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }
        
        private string _passwordVisibleImage;
        public string PasswordVisibleImage
        {
            get => _passwordVisibleImage;
            set => SetProperty(ref _passwordVisibleImage, value);
        }

        private bool _isPassword;
        public bool IsPassword
        {
            get => _isPassword;
            set => SetProperty(ref _isPassword, value);
        }

        private string _passwordWrongText;
        public string PasswordWrongText
        {
            get => _passwordWrongText;
            set => SetProperty(ref _passwordWrongText, value);
        }

        private string _confirmPassword;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        private string _confirmPasswordVisibleImage;
        public string ConfirmPasswordVisibleImage
        {
            get => _confirmPasswordVisibleImage;
            set => SetProperty(ref _confirmPasswordVisibleImage, value);
        }

        private bool _isConfirmPassword;
        public bool IsConfirmPassword
        {
            get => _isConfirmPassword;
            set => SetProperty(ref _isConfirmPassword, value);
        }

        private string _confirmPasswordWrongText;
        public string ConfirmPasswordWrongText
        {
            get => _confirmPasswordWrongText;
            set => SetProperty(ref _confirmPasswordWrongText, value);
        }

        private bool _isButtonEnabled;
        public bool IsButtonEnabled
        {
            get => _isButtonEnabled;
            set => SetProperty(ref _isButtonEnabled, value);
        }

        private ICommand _goBackTapCommand;
        public ICommand GoBackTapCommand =>
            _goBackTapCommand ??= new DelegateCommand(OnGoBackTapAsync);

        private ICommand _passwordVisibleTapCommand;
        public ICommand PasswordVisibleTapCommand =>
            _passwordVisibleTapCommand ??= new DelegateCommand(OnPasswordVisibleTap);

        private ICommand _confirmPasswordVisibleTapCommand;
        public ICommand ConfirmPasswordVisibleTapCommand =>
            _confirmPasswordVisibleTapCommand ??= new DelegateCommand(OnConfirmPasswordVisibleTap);

        private ICommand _createAccountTapCommand;
        public ICommand CreateAccountTapCommand => 
            _createAccountTapCommand ??= new DelegateCommand(OnCreateAccountTapAsync);

        private ICommand _logInWithGoogleTapCommand;
        public ICommand LogInWithGoogleTapCommand =>
            _logInWithGoogleTapCommand ??= new DelegateCommand(OnGoogleLoginClicked);

        private ICommand _logInWithFacebookTapCommand;
        public ICommand LogInWithFacebookTapCommand =>
            _logInWithFacebookTapCommand ??= new DelegateCommand(OnFacebookLoginClicked);

        #endregion

        #region --- Overrides ---

        [Obsolete]
        void OnGoogleLoginClicked()
        {
            string clientId = null;
            string redirectUri = null;

            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    clientId = Constan.GoogleiOSClientId;
                    redirectUri = Constan.GoogleiOSRedirectUrl;
                    break;

                case Device.Android:
                    clientId = Constan.GoogleAndroidClientId;
                    redirectUri = Constan.GoogleAndroidRedirectUrl;
                    break;
            }
            if (account != null)
            {
                account = store.FindAccountsForService(Constan.AppName).FirstOrDefault();
            }
            var authenticator = new OAuth2Authenticator(
                clientId,
                null,
                Constan.GoogleScope,
                new Uri(Constan.GoogleAuthorizeUrl),
                new Uri(redirectUri),
                new Uri(Constan.GoogleAccessTokenUrl),
                null,
                true);

            authenticator.Completed += OnAuthCompleted;
            authenticator.Error += OnAuthError;

            AuthenticationState.Authenticator = authenticator;

            var presenter = new Xamarin.Auth.Presenters.OAuthLoginPresenter();
            presenter.Login(authenticator);
        }

        [Obsolete]
        void OnFacebookLoginClicked()
        {
            string clientId = null;
            string redirectUri = null;

            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    clientId = Constan.FacebookiOSClientId;
                    redirectUri = Constan.FacebookiOSRedirectUrl;
                    break;

                case Device.Android:
                    clientId = Constan.FacebookAndroidClientId;
                    redirectUri = Constan.FacebookAndroidRedirectUrl;
                    break;
            }

            account = store.FindAccountsForService(Constan.AppName).FirstOrDefault();

            var authenticator = new OAuth2Authenticator(
                clientId,
                Constan.FacebookScope,
                new Uri(Constan.FacebookAuthorizeUrl),
                new Uri(Constan.FacebookAccessTokenUrl),
                null);

            authenticator.Completed += OnAuthCompleted;
            authenticator.Error += OnAuthError;

            AuthenticationState.Authenticator = authenticator;

            var presenter = new Xamarin.Auth.Presenters.OAuthLoginPresenter();
            presenter.Login(authenticator);
        }

        [Obsolete]
        async void OnAuthCompleted(object sender, AuthenticatorCompletedEventArgs e)
        {
            var authenticator = sender as OAuth2Authenticator;
            if (authenticator != null)
            {
                authenticator.Completed -= OnAuthCompleted;
                authenticator.Error -= OnAuthError;
            }


            if (e.IsAuthenticated)
            {
                if (authenticator.AuthorizeUrl.Host == "www.facebook.com")
                {
                    FacebookEmail facebookEmail = null;

                    var httpClient = new HttpClient();

                    var json = await httpClient.GetStringAsync($"https://graph.facebook.com/me?fields=id,name,first_name,last_name,email,picture.type(large)&access_token=" + e.Account.Properties["access_token"]);

                    facebookEmail = JsonConvert.DeserializeObject<FacebookEmail>(json);

                    await store.SaveAsync(account = e.Account, Constan.AppName);


                    await NavigationService.NavigateAsync($"/{nameof(NavigationPage)}/{nameof(MainMapTabbedPage)}");
                }
                else
                {
                    User user = null;
                    var request = new OAuth2Request("GET", new Uri(Constan.GoogleUserInfoUrl), null, e.Account);
                    var response = await request.GetResponseAsync();
                    if (response != null)
                    {

                        string userJson = await response.GetResponseTextAsync();
                        user = JsonConvert.DeserializeObject<User>(userJson);
                    }

                    if (account != null)
                    {
                        store.Delete(account, Constan.AppName);
                    }

                    await store.SaveAsync(account = e.Account, Constan.AppName);

                    await NavigationService.NavigateAsync($"/{nameof(NavigationPage)}/{nameof(MainMapTabbedPage)}");
                }
            }
        }

        [Obsolete]
        void OnAuthError(object sender, AuthenticatorErrorEventArgs e)
        {
            var authenticator = sender as OAuth2Authenticator;
            if (authenticator != null)
            {
                authenticator.Completed -= OnAuthCompleted;
                authenticator.Error -= OnAuthError;
            }

            Debug.WriteLine("Authentication error: " + e.Message);
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            parameters.TryGetValue(nameof(UserModel), out _userModel);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            base.OnPropertyChanged(args);

            if (string.IsNullOrWhiteSpace(Password) ||
                string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                IsButtonEnabled = false;
            }
            else
            {
                IsButtonEnabled = true;
            }
        }

        #endregion

        #region --- Private helpers ---

        private bool HasValidPassword()
        {
            bool isPasswordValid = true;
            if (!Validator.HasValidPassword(Password))
            {
                PasswordWrongText = Resource["HasValidPassword"];
                isPasswordValid = false;
            }
            else
            {
                PasswordWrongText = string.Empty;
            }
            return isPasswordValid;
        }

        private bool HasValidConfirmPassword()
        {
            bool isPasswordValid = true;
            if (!Validator.HasEqualPasswords(Password, ConfirmPassword))
            {
                ConfirmPasswordWrongText = Resource["HasMatchPasswords"];
                isPasswordValid = false;
            }
            else
            {
                ConfirmPasswordWrongText = string.Empty;
            }
            return isPasswordValid;
        }

        private UserModel CreateUser()
        {
            var user = _userModel;
            if (!Password.Equals(_userModel.Name))
            {
                PasswordWrongText = string.Empty;
                user.Password = Password;
            }
            else
            {
                user = null;
                PasswordWrongText = Resource["HasConsidePasswordWithName"];
            }

            return user;
        }

        private async void OnGoBackTapAsync()
        {
            await NavigationService.GoBackAsync();
        }

        private void OnPasswordVisibleTap()
        {
            IsPassword = !IsPassword;
        }

        private void OnConfirmPasswordVisibleTap()
        {
            IsConfirmPassword = !IsConfirmPassword;
        }

        private async void OnCreateAccountTapAsync()
        {
            if (HasValidPassword() & HasValidConfirmPassword())
            {
                var user = CreateUser();
                if (user != null)
                {
                    var parameters = new NavigationParameters();
                    await _authorizationService.CreateAccountAsync(_userModel);
                    parameters.Add(nameof(UserModel), _userModel);
                    await NavigationService.NavigateAsync($"/{nameof(NavigationPage)}/{nameof(WelcomePage)}/{nameof(LogInPage)}", parameters);
                }
            }
        }

        #endregion

    }
}
