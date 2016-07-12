using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rtlSdrUWP.rds {
    public static class Data {
        static char[] CharMap = new char[] {
            ' ','!','\\','#','¤','%','&','\'','(',')','*','+',',','-','.','/',
            '0','1','2','3','4','5','6','7','8','9',':',';','<','=','>','?',
            '@','A','B','C','D','E','F','G','H','I','J','K','L','M','N','O',
            'P','Q','R','S','T','U','V','W','X','Y','Z','[','\\',']','―','_',
            '‖','a','b','c','d','e','f','g','h','i','j','k','l','m','n','o',
            'p','q','r','s','t','u','v','w','x','y','z','{','|','}','¯',' ',
            'á','à','é','è','í','ì','ó','ò','ú','ù','Ñ','Ç','Ş','β','¡','Ĳ',
            'â','ä','ê','ë','î','ï','ô','ö','û','ü','ñ','ç','ş','ǧ','ı','ĳ',
            'ª','α','©','‰','Ǧ','ě','ň','ő','π','€','£','$','←','↑','→','↓',
            'º','¹','²','³','±','İ','ń','ű','µ','¿','÷','°','¼','½','¾','§',
            'Á','À','É','È','Í','Ì','Ó','Ò','Ú','Ù','Ř','Č','Š','Ž','Ð','Ŀ',
            'Â','Ä','Ê','Ë','Î','Ï','Ô','Ö','Û','Ü','ř','č','š','ž','đ','ŀ',
            'Ã','Å','Æ','Œ','ŷ','Ý','Õ','Ø','Þ','Ŋ','Ŕ','Ć','Ś','Ź','Ŧ','ð',
            'ã','å','æ','œ','ŵ','ý','õ','ø','þ','ŋ','ŕ','ć','ś','ź','ŧ',' '
        };

        static string[] PtyNames = new string[] {
           "No PTY", "News", "Current Affairs", "Information",
           "Sport", "Education", "Drama", "Cultures",
           "Science", "Varied Speech","Pop Music", "Rock Music",
           "Easy Listening","Light Classics M","Serious Classics","Other Music",
           "Weather & Metr", "Finance", "Children's Progs", "Social Affairs",
           "Religion", "Phone In", "Travel & Touring", "Leisure & Hobby",
           "Jazz Music", "Country Music", "National Music", "Oldies Music",
           "Folk Music", "Documentary", "Alarm Test", "Alarm - Alarm !",
        };

        public static char GetLcdChar(int code) {
            char result = ' ';
            int idx = code - 32;
            if (idx >= 0 && idx < CharMap.Length)
                result = CharMap[idx];
            return result;
        }

        public static string GetPtyName(int pty) {
            return PtyNames[pty];
        }
    }
}
