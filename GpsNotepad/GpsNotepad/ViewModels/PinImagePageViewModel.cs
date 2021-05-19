using GpsNotepad.Models;
using GpsNotepad.Services.Localization;
using Prism.Commands;
using Prism.Navigation;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace GpsNotepad.ViewModels
{
    class PinImagePageViewModel : BaseViewModel
    {
        public PinImagePageViewModel(INavigationService navigationService, 
                                      ILocalizationService localizationService
                                      ) : base(navigationService, localizationService)
        {
        }

        #region --- Public properties ---

        private int _imagePosition;
        public int ImagePosition
        {
            get => _imagePosition;
            set => SetProperty(ref _imagePosition, value);
        }

        private string _imagePositionLabel;
        public string ImagePositionLabel
        {
            get => _imagePositionLabel;
            set => SetProperty(ref _imagePositionLabel, value);
        }

        private ICommand _goBackTapCommand;
        public ICommand GoBackTapCommand =>
            _goBackTapCommand ??= new DelegateCommand(OnGoBackTapAsync);

        #endregion

        #region --- Overrides ---

        #endregion

        #region --- Private helpers ---

        private async void OnGoBackTapAsync()
        {
            await NavigationService.GoBackAsync();
        }

        #endregion

    }
}
