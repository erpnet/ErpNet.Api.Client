using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ErpNet.Api.Client.DomainApi
{
    /// <summary>
    /// Represents a container of texts in different languages.
    /// </summary>
    public partial class MultilanguageString : ComplexTypeResource
    {
        ///
        public MultilanguageString() : base(null) { }

        ///
        public MultilanguageString(params string[] languagePairs)
            : base(Enumerable.Range(0, Math.Max(0, languagePairs.Length - 1))
                  .ToDictionary(i => languagePairs[i], i => (object?)languagePairs[i + 1]))
        { }

        /// <summary>
        /// Gets the string for the language of the current UI culture.
        /// </summary>
        /// <value>
        /// The current string.
        /// </value>
        public string? CurrentString
        {
            get
            {
                var data = RawData();
                var ln = CurrentLanguage;
                if (data.TryGetValue(ln, out var value))
                    return (string?)value;
                return null;
            }
            set
            {
                RawData()[CurrentLanguage] = value;
            }
        }


        /// <summary>
        /// Gets the two letter code of the current thread language.
        /// </summary>
        /// <value>
        /// The current language.
        /// </value>
        public static string CurrentLanguage
        {
            get
            {
                return ValidateAndFormatLanguageKey(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            }
        }

        /// <summary>
        /// Gets any language string, that has a value. 
        /// The method checks the current string first, but if it is empty, returns any string.
        /// </summary>
        /// <value>
        /// Any language string with a value or null if there are no language strings with value.
        /// </value>
        public string? AnyString
        {
            get
            {
                var data = RawData();
                var ln = CurrentLanguage;
                if (data.TryGetValue(ln, out var value))
                    return (string?)value;
                return (string?)data.FirstOrDefault().Value;
            }
        }

        /// <summary>
        /// Validates the and formats the specified language key.
        /// </summary>
        /// <param name="languageKey">The language key to validate.</param>
        /// <returns>The uniformly formatted language key.</returns>
        /// <exception cref="global::System.ArgumentNullException">When Language Key is null.</exception>
        /// <exception cref="global::System.ArgumentException">Language Key should be exactly 2 characters.</exception>
        public static string ValidateAndFormatLanguageKey(string languageKey)
        {
            if (languageKey == null)
                throw new ArgumentNullException("languageKey");
            if (languageKey.Length != 2)
                throw new ArgumentException("Language Key should be exactly 2 characters.");
            return languageKey.ToUpperInvariant();
        }

        /// <summary>
        /// Returns true if the specified value is contained in any of the languages present in this multi-language instance.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Contains(string value)
        {
            foreach (var entry in Values())
                if (entry.Value?.ToString()?.Contains(value) == true)
                    return true;
            return false;
        }
    }
}
