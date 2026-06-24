using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.Description.Translation
{
    public class TranslatorFactory
    {
        static ConcurrentDictionary<String, Translator> translators = new ConcurrentDictionary<string, Translator>();

        public static ITranslator GetTranslator(String groupUri,String action, String messageStyle)
        {
            string translatorKey = $"{groupUri}>{action}>{messageStyle}";

            if (!translators.ContainsKey(translatorKey))

                translators.AddOrUpdate(translatorKey, new Translator(groupUri, action, messageStyle), (a, t) => t);

            return translators[translatorKey];
        }
    }
}
