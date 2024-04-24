using Newtonsoft.Json.Linq;
using System.Text;
using System.Data.SQLite;
using HtmlAgilityPack;
using System.IO;

string connectionString = @"Data Source=C:\Users\barbo\Onedrive\Desktop\English3000Words.db;";

////SQLiteConnection connection = new SQLiteConnection(connectionString);
////connection.Open();
////SQLiteCommand cmd = new SQLiteCommand("Select word from words", connection);
////SQLiteDataReader reader = cmd.ExecuteReader();
////for(int i = 0; reader.Read(); i++)
////{
////    string? kelime = reader["word"].ToString();
////    if(int.TryParse(kelime, out int result) || kelime?.Length <= 2)
////    {
////        Console.WriteLine(kelime);
////        SQLiteCommand sil = new SQLiteCommand($"Delete from words where word = '{kelime}'", connection);
////        sil.ExecuteNonQuery();

////    }

////}
////connection.Close();


List<string> words = new List<string>();

int iterasyon = 0;

//string? text = Console.ReadLine();
using (SQLiteConnection connection = new SQLiteConnection(connectionString))
{
    connection.Open();
    SQLiteCommand cmd = new SQLiteCommand("Select word from words ", connection);// meaning is null olanları çevir
    SQLiteDataReader reader = cmd.ExecuteReader();
    for (int i = 0; reader.Read(); i++)
    {
        words.Add(reader["word"].ToString().ToLower());
    }

    int kacKelimeCevirildi = 0;

    using (HttpClient client = new HttpClient())
    {
        try
        {
            foreach (var word in words)
            {
                iterasyon++;
                string url = "https://en.glosbe.com/en/tr/";
                // URL'yi doğru bir şekilde oluştur
                string newUrl = url + word;

                // Önceden kontrol: URL geçerli mi?
                if (Uri.IsWellFormedUriString(newUrl, UriKind.Absolute))
                {
                    HttpResponseMessage response = await client.GetAsync(newUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        // Yanıt başarılı mı kontrol et
                        response.EnsureSuccessStatusCode();

                        // HTML içeriğini asenkron olarak oku
                        var html = await response.Content.ReadAsStreamAsync();
                        var htmlDoc = new HtmlDocument();
                        htmlDoc.Load(html);

                        List<string> ceviriler = new List<string>();

                        // XPath sorgusu ile çeviri düğümünü seç
                        var ceviri = htmlDoc.DocumentNode.SelectSingleNode("//strong");
                        string dosyaYolu = "C:\\Users\\barbo\\OneDrive\\Desktop\\sikintiliKelimeler.txt";
                        string dosyaYolu2 = "C:\\Users\\barbo\\OneDrive\\Desktop\\BuNeHataKiNe.txt";

                        if (ceviri != null)
                        {
                         var cevir = ceviri.InnerText.Trim();
                        ceviriler.AddRange(cevir.Split(",".ToLower()));//İsteğe göre sonradan diğer çeviriler de kulanılabilir.


                            if (ceviriler[0] != null)
                        {

                            if (ceviriler[0].ToLower() == word.ToLower())
                            {

                               WriteToFile(dosyaYolu, word);

                                Console.WriteLine(word + " *>" + ceviriler[0] + "***> Sıkıntılı Kelimeler Dosyasına Eklendi");
                            }

                            else
                            {
                                if (IsOnlyLetters(ceviriler[0]))
                                { 
                                    string kelime = ceviriler[0];
                                    if(ceviriler.Count>=2)
                                    {
                                        if (IsOnlyLetters(ceviriler[1]))
                                        {
                                            kelime += " / " + ceviriler[1];
                                        }
                                    }
                                    
                                    SQLiteCommand anlamEkle = new SQLiteCommand($"Update words set meaning= '{ceviriler[0]}' where word = '{word}';", connection);
                                    anlamEkle.Parameters.AddWithValue("@meaning", ceviriler[0]);
                                    anlamEkle.Parameters.AddWithValue("@word", word);

                                    await anlamEkle.ExecuteNonQueryAsync();

                                    kacKelimeCevirildi++;
                                    Console.WriteLine(kacKelimeCevirildi + $". kelime çevrildi {word} -> {ceviriler[0]}");
                                }
                                else
                                {
                                    WriteToFile(dosyaYolu, word);
                                    Console.WriteLine(word + " çevirisi hatalı. " + ceviriler[0]);
                                }
                            }
                        }
                        }
                        else
                        {
                            int deneme = kacKelimeCevirildi;
                            using (HttpClient client2 = new HttpClient())
                            {
                                string apiUrl = $"https://api.mymemory.translated.net/get?q={word}&langpair=en|tr";
                                HttpResponseMessage response2 = await client.GetAsync(apiUrl); // Bekleyerek sonucu al
                                string responseBody = await response2.Content.ReadAsStringAsync(); // Bekleyerek içeriği al
                                dynamic json = JObject.Parse(responseBody);
                                string ceviri2 = json.responseData.translatedText;
                                ceviri2.ToLower();

                                if (ceviri2.ToLower() == word.ToLower())
                                {
                                    WriteToFile(dosyaYolu, word);
                                }

                                else
                                {
                                    if (IsOnlyLetters(ceviri2))
                                    {
                                        SQLiteCommand anlamEkle = new SQLiteCommand($"Update words set meaning= '{ceviri2}' where word = '{word}';", connection);
                                        anlamEkle.Parameters.AddWithValue("@meaning", ceviri2);
                                        anlamEkle.Parameters.AddWithValue("@word", word);

                                        await anlamEkle.ExecuteNonQueryAsync();

                                        kacKelimeCevirildi++;
                                        Console.WriteLine(kacKelimeCevirildi + $". kelime çevrildi {word} -> {ceviri2}");
                                    }
                                    else
                                    {
                                        WriteToFile(dosyaYolu2, word);
                                        Console.WriteLine(word + " çevirisi hatalı. " + ceviri2);
                                        continue; // Döngünün bir sonraki iterasyonuna geç
                                    }

                                }
                            }
                                
                        }
                    }
                    Console.WriteLine(iterasyon + ". Kelimedeyiz.");

                }
            }
            Console.WriteLine("Kelime çevirme işlemi tamamlandı. Toplam " + kacKelimeCevirildi + " kelime çevrildi.");

        }

        catch (Exception e)
        {
            // Diğer hatalar
            Console.WriteLine($"An error occurred: {e.Message}");
        }
    }
            Console.WriteLine("Kelime çevirme işlemi tamamlandı. Toplam " + kacKelimeCevirildi + " kelime çevrildi.");
    }//1798, 500'e kadar kontrol ettim ama bir tane daha çeviri programı bulup oradan da teyit etmek ve farklı anlamda olan varsa yanına / ile yazmak mantıklı olabilir gibi duruyor.
    
           
bool IsOnlyLetters(string input)
{
    foreach (char c in input)
    {
        if (!char.IsLetter(c))
            return false;
    }
    return true;
}
void WriteToFile(string dosyaYolu, string word)
{
    // Dosyanın var olup olmadığını kontrol et
    if (!File.Exists(dosyaYolu))
    {
        // Dosya yoksa, yeni bir dosya oluştur ve kelimeyi yaz
        using (StreamWriter sw = File.CreateText(dosyaYolu))
        {
            sw.WriteLine(word);
        }
    }
    else
    {
        // Dosya varsa, dosyanın sonuna yeni kelimeyi ekle
        using (StreamWriter sw = File.AppendText(dosyaYolu))
        {
            sw.WriteLine(word);
        }
    }
}
        //MyMemort ile çeviri yapma
    //    using (HttpClient client = new HttpClient())
    //    {
    //        foreach (var word in words)
    //        { 
    //        string apiUrl = $"https://api.mymemory.translated.net/get?q={word}&langpair=en|tr";
    //        HttpResponseMessage response = await client.GetAsync(apiUrl); // Bekleyerek sonucu al
    //        string responseBody = await response.Content.ReadAsStringAsync(); // Bekleyerek içeriği al
    //        dynamic json = JObject.Parse(responseBody);
    //        string ceviri = json.responseData.translatedText;
    //            ceviri.ToLower();

//    if (ceviri.ToLower() == word.ToLower())
//    {
//        Console.WriteLine(word + "***");
//    }

//    else
//    {
//        if (IsOnlyLetters(ceviri))
//        {
//            SQLiteCommand anlamEkle = new SQLiteCommand($"Update words set meaning= '{ceviri}' where word = '{word}';", connection);
//            anlamEkle.Parameters.AddWithValue("@meaning", ceviri);
//            anlamEkle.Parameters.AddWithValue("@word", word);

//            await anlamEkle.ExecuteNonQueryAsync();

//            kacKelimeCevirildi++;
//            Console.WriteLine(kacKelimeCevirildi + $". kelime çevrildi {word} -> {ceviri}");
//        }
//        else Console.WriteLine(word + " çevirisi hatalı. " + ceviri);

//    }
//}
                //Console.WriteLine("Kelime çevirme işlemi tamamlandı. Toplam " + kacKelimeCevirildi + " kelime çevrildi.");
//}//1798, 500'e kadar kontrol ettim ama bir tane daha çeviri programı bulup oradan da teyit etmek ve farklı anlamda olan varsa yanına / ile yazmak mantıklı olabilir gibi duruyor.
//bool IsOnlyLetters(string input)
//{
//    foreach (char c in input)
//    {
//        if (!char.IsLetter(c))
//            return false;
//    }
//    return true;
//}


//FreeLang ile çeviri yapma
////using ( var client = new HttpClient())
////{
////    string word = "cat";
////    var url = "https://www.freelang.net/online/turkish.php?lg=gb";
////    var content = new FormUrlEncodedContent(new[]
////    {
////        new KeyValuePair<string, string>("mot1", ""),
////        new KeyValuePair<string, string>("mot2", word),
////        new KeyValuePair<string, string>("dico", "us_tur_eng"),
////        new KeyValuePair<string, string>("entier", "CHECKED")
////    }); 
////    HttpResponseMessage response = await client.PostAsync(url, content);
////    string responseBody = await response.Content.ReadAsStringAsync();

////    var htmlDoc = new HtmlDocument();
////    htmlDoc.LoadHtml(responseBody);

////    // Çeviri sonuçlarını çekmek için doğru XPath sorgusu
////    var resultNode = htmlDoc.DocumentNode.SelectSingleNode("//table//tr[1]//td[2]/font/b");

////    if (resultNode != null)
////    {
////        Console.WriteLine($"Çeviri: {resultNode.InnerText}");
////    }
////    else
////    {
////        Console.WriteLine("Çeviri bulunamadı.");
////    }

////    //Console.WriteLine(responseBody);
////    Console.ReadKey();
////}
///
