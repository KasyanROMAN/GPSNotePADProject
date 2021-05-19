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
    class CreateAccountFirstPageViewModel : BaseViewModel
    {
        Account account;
        [Obsolete]
        AccountStore store;
        private readonly IAuthorizationService _authorizationService;

        public CreateAccountFirstPageViewModel(IAuthorizationService authorizationService,
                                               ILocalizationService localizationService,
                                               INavigationService navigationService) : base(navigationService, localizationService)
        {
            _authorizationService = authorizationService;
            store = AccountStore.Create();
        }

        #region --- Public Properties ---

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _nameWrongText;
        public string NameWrongText
        {
            get => _nameWrongText;
            set => SetProperty(ref _nameWrongText, value);
        }

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

        private bool _isNextButtonEnabled;
        public bool IsNextButtonEnabled
        {
            get => _isNextButtonEnabled;
            set => SetProperty(ref _isNextButtonEnabled, value);
        }

        private ICommand _goBackTapCommand;
        public ICommand GoBackTapCommand =>
            _goBackTapCommand ??= new DelegateCommand(OnGoBackTapAsync);

        private ICommand _nameClearTapCommand;
        public ICommand NameClearTapCommand =>
            _nameClearTapCommand ??= new DelegateCommand(OnNameClearTap);

        private ICommand _emailClearTapCommand;
        public ICommand EmailClearTapCommand =>
            _emailClearTapCommand ??= new DelegateCommand(OnEmailClearTap);

        private ICommand _nextTapCommand;
        public ICommand NextTapCommand => 
            _nextTapCommand ??= new DelegateCommand(OnNextTapAsync);

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
        protected override void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            base.OnPropertyChanged(args);

            if (string.IsNullOrWhiteSpace(Name) ||
                string.IsNullOrWhiteSpace(Email))
            {
                IsNextButtonEnabled = false;
            }
            else
            {
                IsNextButtonEnabled = true;
            }
        }

        #endregion

        #region --- Private helpers ---

        private bool HasValidName()
        {
            bool isNameValid;
            if (!Validator.HasValidName(Name))
            {
                NameWrongText = Resource["HasValidName"];
                isNameValid = false;
            }
            else
            {
                NameWrongText = string.Empty;
                isNameValid = true;
            }
            return isNameValid;
        }

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

        private UserModel CreateUser()
        {
            UserModel userModel = new UserModel()
            {
                Name = Name,
                Email = Email
            };

            return userModel;
        }

        private async void OnGoBackTapAsync()
        {
            await NavigationService.GoBackAsync();
        }

        private void OnNameClearTap()
        {
            Name = string.Empty;
        }

        private void OnEmailClearTap()
        {
            Email = string.Empty;
        }

        private async void OnNextTapAsync()
        {
            if (HasValidName() &
                HasValidEmail())
            {
                var userModel = CreateUser();
                if (userModel != null)
                {
                    var isBusyEmail = await _authorizationService.HasEmailAsync(Email);
                    if (!isBusyEmail)
                    {
                        EmailWrongText = string.Empty;
                        var parameters = new NavigationParameters();
                        parameters.Add(nameof(UserModel), userModel);
                        await NavigationService.NavigateAsync(nameof(CreateAccountSecondPage), parameters);
                    }
                    else
                    {
                        EmailWrongText = Resource["HasBusyEmail"];
                    }
                }
            }
        }

    

        #endregion

    }
}
