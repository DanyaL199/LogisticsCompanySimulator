import os
from pathlib import Path

def combine_cs_to_single_txt(source_dir, output_filename):
    # Вказуємо шлях до папки
    path = Path(source_dir)
    
    if not path.exists():
        print(f"Папку '{source_dir}' не знайдено!")
        return

    # Шукаємо всі файли .cs
    cs_files = list(path.rglob("*.cs"))

    if not cs_files:
        print("Файлів з розширенням .cs не знайдено.")
        return

    # Шлях до загального файлу, який буде створено
    output_path = path / output_filename

    # Відкриваємо загальний файл для запису
    with open(output_path, 'w', encoding='utf-8') as outfile:
        for cs_file in cs_files:
            content = ""
            # Спроба 1: Читаємо як UTF-8
            try:
                with open(cs_file, 'r', encoding='utf-8') as f:
                    content = f.read()
            # Спроба 2: Якщо вилітає помилка кодування, читаємо як Windows-1251
            except UnicodeDecodeError:
                try:
                    with open(cs_file, 'r', encoding='windows-1251') as f:
                        content = f.read()
                except Exception as e:
                    print(f"Не вдалося прочитати {cs_file.name}: {e}")
                    continue
            except Exception as e:
                print(f"Загальна помилка читання {cs_file.name}: {e}")
                continue

            # Записуємо заголовок та код у загальний файл
            outfile.write(f"Назва файла: {cs_file.name}\n\n")
            outfile.write(content)
            # Додаємо візуальний розділювач між файлами
            outfile.write("\n\n" + "="*50 + "\n\n") 
            
            print(f"Додано до загального файлу: {cs_file.name}")

    print(f"\nГОТОВО! Всі скрипти збережено у файл:\n{output_path}")

# Ваш точний шлях до папки
folder_path = r"E:\Unity project\LogisticsCompanySimulator\Assets\Scripts" 
# Назва єдиного файлу, який ми отримаємо на виході
output_name = "All_Scripts_Combined.txt"

combine_cs_to_single_txt(folder_path, output_name)