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
    class LogInPageViewModel : BaseViewModel
    {
        Account account;
        [Obsolete]
        AccountStore store;


        private readonly IAuthorizationService _authorizationService;
      

        public LogInPageViewModel(INavigationService navigationService,
                                  ILocalizationService localizationService,
                                  IAuthorizationService authorizationService) : base(navigationService, localizationService)
        {
            _authorizationService = authorizationService;
            _isPassword = true;
            store = AccountStore.Create();
        }

        #region --- Public Properties ---

        private string _email;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _emailWrongText;
        public string EmailWrongText
        {
            get => _emailWrongText;
            set => SetProperty(ref _emailWrongText, value);
        }

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

        private bool _isButtonEnable;
        public bool IsButtonEnable
        {
            get => _isButtonEnable;
            set => SetProperty(ref _isButtonEnable, value);
        }

        private ICommand _goBackTapCommand;
        public ICommand GoBackTapCommand =>
            _goBackTapCommand ??= new DelegateCommand(OnGoBackTapAsync);

        private ICommand _emailClearTapCommand;
        public ICommand EmailClearTapCommand =>
            _emailClearTapCommand ??= new DelegateCommand(OnEmailClearTap);

        private ICommand _passwordVisibleTapCommand;
        public ICommand PasswordVisibleTapCommand =>
            _passwordVisibleTapCommand ??= new DelegateCommand(OnPasswordVisibleTap);

        private ICommand _logInTapCommand;
        public ICommand LogInTapCommand =>
            _logInTapCommand ??= new DelegateCommand(OnLogInTapAsync);

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

                    // If the user is authenticated, request their basic user data from Google
                    // UserInfoUrl = https://www.googleapis.com/oauth2/v2/userinfo
                    var request = new OAuth2Request("GET", new Uri(Constan.GoogleUserInfoUrl), null, e.Account);
                    var response = await request.GetResponseAsync();
                    if (response != null)
                    {
                        // Deserialize the data and store it in the account store
                        // The users email address will be used to identify data in SimpleDB
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
            if (parameters.TryGetValue<UserModel>(nameof(UserModel), out var userModel))
            {
                Email = userModel.Email;
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            base.OnPropertyChanged(args);

            if (string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password))
            {
                IsButtonEnable = false;
            }
            else
            {
                IsButtonEnable = true;
            }
        }

        #endregion

        #region --- Private helpers ---

        private bool HasValidEmail()
        {
            bool isEmailValid;
            if (!Validator.HasValidEmail(Email))
            {
                EmailWrongText = Resource["HasValidEmail"];
                isEmailValid = false;
            }
            else
            {
                EmailWrongText = string.Empty;
                isEmailValid = true;
            }
            return isEmailValid;
        }

        private bool HasValidPassword()
        {
            bool isPasswordValid;
            if (!Validator.HasValidPassword(Password))
            {
                PasswordWrongText = Resource["HasValidPassword"];
                isPasswordValid = false;
            }
            else
            {
                PasswordWrongText = string.Empty;
                isPasswordValid = true;
            }
            return isPasswordValid;
        }

        private async void OnGoBackTapAsync()
        {
            await NavigationService.GoBackAsync();
        }

        private void OnEmailClearTap()
        {
            Email = string.Empty;
        }

        private void OnPasswordVisibleTap()
        {
            IsPassword = !IsPassword;
        }

        private async void OnLogInTapAsync()
        {
            if (HasValidEmail() && HasValidPassword())
            {
                var isAuthorized = await _authorizationService.LogInAsync(Email, Password);
                if (isAuthorized)
                {
                    await NavigationService.NavigateAsync($"/{nameof(NavigationPage)}/{nameof(MainMapTabbedPage)}");
                }
            }
            
        }

      

        #endregion

    }
}
