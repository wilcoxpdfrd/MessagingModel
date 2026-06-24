using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Http
{
    public class AcceptCharsetHeaderHelper
    {
        public static Encoding SelectAcceptableCharset(String[] accepts)
        {
            if (accepts == null || accepts.Length == 0)

                return Encoding.UTF8;

            List<(Double Quality, Encoding Charset)> ps = new List<(double Quality, Encoding Accept)>();

            foreach (String accept in accepts)
            {
                Encoding charset = ParseAccept(accept, out double quality);

                if (charset != null)
                {
                    ps.Add((quality, charset));
                }
            }

            IEnumerable<(Double Quality, Encoding Charset)> ordered = ps.OrderByDescending(t => t.Quality);

            double highestQuality = ordered.First().Quality;

            IEnumerable<Encoding> highest = ordered.TakeWhile(t => t.Quality == highestQuality).Select(t => t.Charset);

            Encoding acceptable = highest.FirstOrDefault(c => c.HeaderName == Encoding.UTF8.HeaderName);

            if (acceptable != null)

                return acceptable;

            acceptable = highest.FirstOrDefault(c => c.HeaderName == Encoding.UTF8.HeaderName);

            return highest.First();
        }

        /// <summary>
        /// Parses an Accept-Charset header value
        /// </summary>
        /// <param name="accept"></param>
        /// <param name="quality"></param>
        /// <returns>media-type</returns>
        public static Encoding ParseAccept(string accept, out double quality)
        {
            if (accept == null)
            {
                quality = 0;

                return null;
            }

            String[] acceptSegments = accept.Split(';');

            quality = 1;

            for (int i = 1; i < acceptSegments.Length; i++)
            {
                string acceptSegment = acceptSegments[i].Trim().ToLower();

                String[] acceptSegmentParts = acceptSegment.Split('=');

                if (acceptSegmentParts.Length > 1)
                {
                    switch (acceptSegmentParts[0])
                    {
                        case "q":

                            double.TryParse(acceptSegmentParts[1], out quality);

                            break;
                    }
                }
            }

            if (acceptSegments[0] == "*")

                return Encoding.UTF8;

            return Encoding.GetEncoding(acceptSegments[0].ToLower());
        }
    }
}
