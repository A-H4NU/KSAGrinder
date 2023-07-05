using System;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace KSAGrinder.Properties
{
    // https://kaki104.tistory.com/718
    public class StringResource : DynamicObject
    {
        public event EventHandler<string> LanguageChanged;
        private readonly ResourceManager _resourceManager;
        private CultureInfo _cultureInfo;

        public StringResource()
        {
            _resourceManager = Strings.StringResources.ResourceManager;
        }

        public string this[string id]
        {
            get
            {
                if (String.IsNullOrEmpty(id)) return null;
                string str = _resourceManager.GetString(id, _cultureInfo);
                if (String.IsNullOrEmpty(str))
                {
                    str = id;
                }
                return str;
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            string id = binder.Name;
            string str = _resourceManager.GetString(id, _cultureInfo);
            if (String.IsNullOrEmpty(str))
            {
                str = id;
            }
            result = str;
            return true;
        }

        public void ChangeLanguage(string languageCode)
        {
            _cultureInfo = new CultureInfo(languageCode);
            Thread.CurrentThread.CurrentCulture = _cultureInfo;
            Thread.CurrentThread.CurrentUICulture = _cultureInfo;
            foreach (Window window in Application.Current.Windows.Cast<Window>())
            {
                if (!window.AllowsTransparency)
                {
                    window.Language = XmlLanguage.GetLanguage(_cultureInfo.Name);
                }
            }

            LanguageChanged?.Invoke(this, _cultureInfo.Name);
        }
    }
}
