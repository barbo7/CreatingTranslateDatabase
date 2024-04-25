using Newtonsoft.Json.Linq;
using System.Text;
using System.Data.SQLite;
using HtmlAgilityPack;
using System.IO;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;

namespace YourNamespace
{
    public class YourClass
    {
        private static readonly string connectionString = @"Data Source=C:\Users\barbo\Onedrive\Desktop\English3000Words.db";
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task Main(string[] args)
        {

            string dosyaYolu= "C:\\Users\\barbo\\OneDrive\\Desktop\\SorunluKelimeler.txt";    
            HashSet<string> words = new HashSet<string>(); /*GetWordsFromDatabase();*/

            using (StreamReader sr = new StreamReader(dosyaYolu))
            {
                string line;
                // Dosyanın sonuna kadar her satırı okuyun
                while ((line = sr.ReadLine()) != null)
                {
                    // Okunan satırı listeye ekleyin
                    words.Add(line.Trim(' '));
                }
            }
            List<string> wordsList = new List<string>();

            int translatedWordsCount = 0;
            int indeks = 0;
            int kacTaneVar = 0;

            foreach (var word in words)
            {
                try
                {
                    string translation = await TranslateWord(word);
                   
                    if (string.IsNullOrEmpty(translation))
                    {
                        //WriteToFile(dosyaYolu, word);
                        wordsList.Add(word);
                        Console.WriteLine($"{word} çevirisi hatalı.");
                    }
                    else
                    {
                        bool kelimeVarMi = false;

                        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();
                            SQLiteCommand cmd = new SQLiteCommand("SELECT word, meaning FROM words where word=@word", connection);
                            cmd.Parameters.AddWithValue("@word", word);

                            SQLiteDataReader reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                if (reader["meaning"].ToString() == translation)
                                {
                                    Console.WriteLine($"{word} zaten veritabanında mevcut.");
                                    kacTaneVar++;
                                    kelimeVarMi = true;
                                    break;
                                }
                            }
                        }
                        if (!kelimeVarMi)
                        {
                            UpdateWordMeaningInDatabase(word, translation);
                            translatedWordsCount++;
                            Console.WriteLine($"{translatedWordsCount}. kelime çevrildi: {word} -> {translation}");
                        }
                    }
                    indeks++;
                    Console.WriteLine(indeks + ". satırdayız");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Hata oluştu: {e.Message}");
                }
            }

            File.WriteAllLines(dosyaYolu, wordsList);// eğer tüm listeyi silip tekrar oluşturmak istiyorsak.

            //// Dosyayı açın ve yazma modunda (append) açın
            //using (StreamWriter writer = File.AppendText(dosyaYolu))
            //{
            //    // Güncellenmiş satırları dosyaya yazın
            //    foreach (string line in wordsList)
            //    {
            //        writer.WriteLine(line);
            //    }
            //}

            Console.WriteLine($"Kelime çevirme işlemi tamamlandı. Toplam {translatedWordsCount} kelime çevrildi.");
            Console.WriteLine(kacTaneVar + " tane kelime zaten veritabanında mevcuttu.");
        }
      

        private static List<string> GetWordsFromDatabase()
        {
            List<string> words = new List<string>();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                SQLiteCommand cmd = new SQLiteCommand("SELECT word, meaning FROM words where meaning is null or meaning=word", connection);
                SQLiteDataReader reader = cmd.ExecuteReader();
                int kacTane = 0;
                while (reader.Read())
                {
                        kacTane++;
                        words.Add(reader["word"].ToString());
                }
                Console.WriteLine(kacTane + " tane kelime var");
            }

            return words;
        }


        private static async Task<string> TranslateWord(string word)
        {
            word.ToLower();
            string url = $"https://en.glosbe.com/en/tr/{word}";
            HttpResponseMessage response = await httpClient.GetAsync(url);
            string? result = null;

            if (response.IsSuccessStatusCode)
            {
                var html = await response.Content.ReadAsStreamAsync();
                var htmlDoc = new HtmlDocument();
                htmlDoc.Load(html);

                List<string> translations = new List<string>();
                var translationNode = htmlDoc.DocumentNode.SelectSingleNode("//strong");
                if (translationNode != null)
                {
                    string translation = translationNode.InnerText.Trim();

                    // Alınan verileri temizlemek için HtmlDecode fonksiyonunu kullanarak gerçek karakterlere dönüştürün
                    string decodedTranslation = WebUtility.HtmlDecode(translation);

                    translations.AddRange(decodedTranslation.Split(","));
                    foreach(var i in translations)
                    {
                        if (ContainsSymbolExceptSpaceAndComma(i))
                        {
                            // Eğer sembol içeren bir çeviri bulunursa, alternatif çeviri servisine yönlendir
                            return await TranslateWord2(word);
                        }

                    }
                    result = translations[0];
                    if (translations.Count > 1)
                        result = translations[0] + "," + translations[1];
                }
            }
            else if (!response.IsSuccessStatusCode || result == null)
            {
                result = await TranslateWord2(word);
            }

            return result;

        }

    private static async Task<string> TranslateWord2(string word)
    {
        using (HttpClient client = new HttpClient())
        {
            string apiUrl = $"https://api.mymemory.translated.net/get?q={word}&langpair=en|tr";
            HttpResponseMessage response2 = await client.GetAsync(apiUrl); // Bekleyerek sonucu al
            string responseBody = await response2.Content.ReadAsStringAsync(); // Bekleyerek içeriği al
            dynamic json = JObject.Parse(responseBody);
            string ceviri2 = json.responseData.translatedText;
            int kacKelime = ceviri2.Split(" ").Count();
            if (IsOnlyLetters(ceviri2) && ceviri2 != "." && ceviri2 != "NA" && ceviri2 != word && kacKelime <= 2)
            {
                return ceviri2.ToLower();
            }
            return null;
        }
    }

        private static void UpdateWordMeaningInDatabase(string word, string meaning)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                SQLiteCommand cmd = new SQLiteCommand($"UPDATE words SET meaning = @meaning WHERE word = @word ", connection);
                cmd.Parameters.AddWithValue("@meaning", meaning);
                cmd.Parameters.AddWithValue("@word", word);
                cmd.ExecuteNonQuery();
            }
        }

        private static void WriteToFile(string filePath, string word)
        {
            using (StreamWriter sw = File.AppendText(filePath))
            {
                sw.WriteLine(word);
            }
        }
        private static bool ContainsSymbolExceptSpaceAndComma(string input)
        {
            foreach (char c in input)
            {
                if (!char.IsLetterOrDigit(c) && c != ' ' && c != ',')
                {
                    // Eğer karakter bir sembol (boşluk veya virgül hariç) ise, true döndür ve döngüyü sonlandır
                    return true;
                }
            }
            // Eğer metin içinde sembol (boşluk veya virgül hariç) bulunmazsa, false döndür
            return false;
        }

        private static bool IsOnlyLetters(string input)
        {
            foreach (char c in input)
            {
                if (!char.IsLetter(c))
                    return false;
            }
            return true;
        }


    }
}
