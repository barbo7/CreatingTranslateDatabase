import sqlite3
import re
import fitz  # PyMuPDF kullanarak PDF dosyasını işleme
from pathlib import Path

def create_connection(db_file):
    """ Veritabanı bağlantısı oluştur """
    try:
        conn = sqlite3.connect(db_file)
        print("Veritabanı başarıyla oluşturuldu ve bağlantı yapıldı.")
        return conn
    except sqlite3.Error as e:
        print(f"Hata: {e}, Veritabanı bağlantısı başarısız. Yol: {db_file}")
        return None

def create_table(conn):
    """ Kelimeleri saklamak için bir tablo oluştur """
    sql_create_words_table = """ CREATE TABLE IF NOT EXISTS words (
                                        id integer PRIMARY KEY,
                                        word text NOT NULL UNIQUE,
                                        meaning text NULL
                                    ); """
    try:
        c = conn.cursor()
        c.execute(sql_create_words_table)
    except sqlite3.Error as e:
        print(f"Tablo oluşturma hatası: {e}")

def add_word(conn, word):
    """ Veritabanına yeni bir kelime ekler """
    sql = ''' INSERT OR IGNORE INTO words(word)
              VALUES(?) '''
    cur = conn.cursor()
    cur.execute(sql, (word,))
    conn.commit()

def extract_words_from_pdf(pdf_path):
    """ PDF dosyasından metin çıkarır ve temizler """
    doc = fitz.open(pdf_path)
    text = ""
    for page in doc:
        text += page.get_text()

    # Köşeli parantezleri ve gramer etiketlerini temizle
    text = re.sub(r"\[.*?\]", "", text)

    # Belirli başlık metinlerini çıkar
    header_pattern = r"The Oxford 5000™ by CEFR level \(American English\)"
    text = re.sub(header_pattern, "", text)

    # Sayfa numaralarını ve başlık tekrarlarını çıkar
    page_info_pattern = r"© Oxford University Press \d+ / \d+"
    text = re.sub(page_info_pattern, "", text)

    # Cümleleri ve sonunda nokta olan kelimeleri hariç tut
    words = re.findall(r"\b\w+\b(?<!\.)", text)
    return [word for word in words if '.' not in word and ' ' not in word]

def main():
    pdf_path = "C:/Users/barbo/OneDrive/Desktop/American_Oxford_5000_by_CEFR_level.pdf"
    database_path = "C:/Users/barbo/OneDrive/Desktop/English5000Words.db"

    print(f"Veritabanı yolu: {database_path}")

    conn = create_connection(database_path)
    if conn:
        create_table(conn)
        print("Kelimeler veritabanına kaydedilecek...")
        words = extract_words_from_pdf(pdf_path)
        for word in words:
            add_word(conn, word.lower())
        conn.close()
        print("Kelimeler başarıyla veritabanına kaydedildi.")
    else:
        print("Veritabanına bağlanılamadı, işlem iptal edildi.")

if __name__ == '__main__':
    main()
