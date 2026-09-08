using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GeneticSearching
{
    // Структура для хранения данных о белке согласно требованию ТЗ
    struct GeneticData
    {
        public string protein;     // Название белка
        public string organism;    // Название организма
        public string amino_acids; // Раскодированная цепочка аминокислот
    }

    class Program
    {
        static void Main(string[] args)
        {
            string sequencesFile = "sequences.txt";
            string commandsFile = "commands.txt";
            string outputFile = "genedata.txt";

            // 1. Чтение и загрузка генетических данных из sequences.txt
            List<GeneticData> geneticDataList = LoadGeneticData(sequencesFile);

            if (geneticDataList.Count == 0)
            {
                Console.WriteLine($"Файл {sequencesFile} не найден или пуст.");
                return;
            }

            if (!File.Exists(commandsFile))
            {
                Console.WriteLine($"Файл команд {commandsFile} не найден.");
                return;
            }

            // 2. Обработка команд из commands.txt и запись результатов в genedata.txt
            using (StreamWriter writer = new StreamWriter(outputFile, false, Encoding.UTF8))
            {
                // Заголовок выходного файла
                writer.WriteLine("Иван Иванов"); // Укажите ваше имя и фамилию
                writer.WriteLine("Генетический поиск");

                string[] commandLines = File.ReadAllLines(commandsFile);
                int operationNumber = 1;

                foreach (string line in commandLines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] parts = line.Split('\t');
                    string command = parts[0].Trim();

                    switch (command)
                    {
                        case "search":
                            if (parts.Length >= 2)
                            {
                                ProcessSearch(operationNumber, parts[1].Trim(), geneticDataList, writer);
                            }
                            break;

                        case "diff":
                            if (parts.Length >= 3)
                            {
                                ProcessDiff(operationNumber, parts[1].Trim(), parts[2].Trim(), geneticDataList, writer);
                            }
                            break;

                        case "mode":
                            if (parts.Length >= 2)
                            {
                                ProcessMode(operationNumber, parts[1].Trim(), geneticDataList, writer);
                            }
                            break;
                    }

                    operationNumber++;
                }
            }

            Console.WriteLine($"Обработка завершена. Результаты сохранены в {outputFile}");
        }

        #region Методы работы с RLE Кодированием/Декодированием

        /// <summary>
        /// Раскодирование RLE-последовательности (например, 3Q -> QQQ, FK3I -> FKIII)
        /// </summary>
        static string RLDecoding(string amino_acids)
        {
            if (string.IsNullOrEmpty(amino_acids)) return amino_acids;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < amino_acids.Length; i++)
            {
                char c = amino_acids[i];
                // Если встретилась цифра от 3 до 9
                if (char.IsDigit(c))
                {
                    int count = c - '0';
                    if (i + 1 < amino_acids.Length)
                    {
                        char nextChar = amino_acids[i + 1];
                        sb.Append(nextChar, count);
                        i++; // Пропускаем следующий символ, так как мы его уже повторили
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Сжатие RLE (серверная/вспомогательная функция, если потребуется зажать цепочку)
        /// </summary>
        static string RLEncoding(string amino_acids)
        {
            if (string.IsNullOrEmpty(amino_acids)) return amino_acids;

            StringBuilder sb = new StringBuilder();
            int i = 0;
            while (i < amino_acids.Length)
            {
                char current = amino_acids[i];
                int runLength = 1;

                while (i + runLength < amino_acids.Length &&
                       amino_acids[i + runLength] == current &&
                       runLength < 9)
                {
                    runLength++;
                }

                if (runLength >= 3)
                {
                    sb.Append(runLength);
                    sb.Append(current);
                }
                else
                {
                    sb.Append(current, runLength);
                }

                i += runLength;
            }
            return sb.ToString();
        }

        #endregion

        #region Загрузка данных

        static List<GeneticData> LoadGeneticData(string filePath)
        {
            List<GeneticData> list = new List<GeneticData>();
            if (!File.Exists(filePath)) return list;

            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split('\t');
                if (parts.Length >= 3)
                {
                    GeneticData data = new GeneticData
                    {
                        protein = parts[0].Trim(),
                        organism = parts[1].Trim(),
                        // Декодируем RLE при загрузке данных
                        amino_acids = RLDecoding(parts[2].Trim())
                    };
                    list.Add(data);
                }
            }
            return list;
        }

        #endregion

        #region Обработка операций

        /// <summary>
        /// Операция search: Поиск подстроки аминокислот
        /// </summary>
        static void ProcessSearch(int opNum, string rawPattern, List<GeneticData> dataList, StreamWriter writer)
        {
            // Раскодируем искомый фрагмент, если он задан в формате RLE (например, FK3I -> FKIII)
            string pattern = RLDecoding(rawPattern);

            writer.WriteLine($"{opNum:D3} search\t{pattern}");
            writer.WriteLine("organism\tprotein");

            bool found = false;
            foreach (var data in dataList)
            {
                if (data.amino_acids.Contains(pattern))
                {
                    writer.WriteLine($"{data.organism}\t{data.protein}");
                    found = true;
                }
            }

            if (!found)
            {
                writer.WriteLine("NOT FOUND");
            }

            writer.WriteLine("--------------------------------------------------");
        }

        /// <summary>
        /// Операция diff: Сравнение двух белков по количеству различающихся аминокислот
        /// </summary>
        static void ProcessDiff(int opNum, string protein1Name, string protein2Name, List<GeneticData> dataList, StreamWriter writer)
        {
            writer.WriteLine($"{opNum:D3} diff\t{protein1Name}\t{protein2Name}");
            writer.WriteLine("amino-acids difference:");

            int p1Index = dataList.FindIndex(d => d.protein == protein1Name);
            int p2Index = dataList.FindIndex(d => d.protein == protein2Name);

            bool p1Missing = p1Index == -1;
            bool p2Missing = p2Index == -1;

            if (p1Missing || p2Missing)
            {
                List<string> missing = new List<string>();
                if (p1Missing) missing.Add(protein1Name);
                if (p2Missing) missing.Add(protein2Name);

                writer.WriteLine($"MISSING: {string.Join(", ", missing)}");
            }
            else
            {
                string seq1 = dataList[p1Index].amino_acids;
                string seq2 = dataList[p2Index].amino_acids;

                int minLen = Math.Min(seq1.Length, seq2.Length);
                int diffCount = 0;

                // Считаем различия в совпадающей по длине части
                for (int i = 0; i < minLen; i++)
                {
                    if (seq1[i] != seq2[i]) diffCount++;
                }

                // Добавляем разницу в длинах цепочек
                diffCount += Math.Abs(seq1.Length - seq2.Length);

                writer.WriteLine(diffCount);
            }

            writer.WriteLine("--------------------------------------------------");
        }

        /// <summary>
        /// Операция mode: Определение самой частой аминокислоты в белке
        /// </summary>
        static void ProcessMode(int opNum, string proteinName, List<GeneticData> dataList, StreamWriter writer)
        {
            writer.WriteLine($"{opNum:D3} mode\t{proteinName}");
            writer.WriteLine("amino-acid\toccurs:");

            int pIndex = dataList.FindIndex(d => d.protein == proteinName);

            if (pIndex == -1)
            {
                writer.WriteLine($"MISSING: {proteinName}");
            }
            else
            {
                string seq = dataList[pIndex].amino_acids;
                Dictionary<char, int> counts = new Dictionary<char, int>();

                foreach (char c in seq)
                {
                    if (counts.ContainsKey(c))
                        counts[c]++;
                    else
                        counts[c] = 1;
                }

                // Находим максимальную частоту; при равенстве — первая по алфавиту
                var top = counts
                    .OrderByDescending(kv => kv.Value)
                    .ThenBy(kv => kv.Key)
                    .First();

                writer.WriteLine($"{top.Key}\t{top.Value}");
            }

            writer.WriteLine("--------------------------------------------------");
        }

        #endregion
    }
}