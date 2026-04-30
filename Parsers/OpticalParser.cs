using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BDCOM.OLT.Manager.Parsers
{
    public static class OpticalParser
    {
        // Для "Оптика ONU"
        public static string GetCleanOnuOptical(string raw)
        {
            string text = Clean(raw);
            var match = Regex.Match(text, @"RxPower\(dBm\)\s*[:\-]?\s*([-\d.]+)", RegexOptions.IgnoreCase);
            if (match.Success)
                return $"interface RxPower(dBm)\n----------- --------------\nepon0/1:1   {match.Groups[1].Value}";

            return text.Trim();
        }

        // Для "Сигналы EPON" — улучшенный парсер с обработкой --More--
        public static List<(int OnuId, double RxPower)> ParsePortOptical(string raw)
        {
            string text = Clean(raw);
            var list = new List<(int, double)>();

            // Более надёжный паттерн
            var matches = Regex.Matches(text, @"epon\d+/\d+:(\d+)\s+([-\d.]+)", RegexOptions.IgnoreCase);

            foreach (Match m in matches)
            {
                if (int.TryParse(m.Groups[1].Value, out int onuId) && 
                    double.TryParse(m.Groups[2].Value, out double power))
                {
                    list.Add((onuId, power));
                }
            }

            return list.OrderBy(x => x.Item1).ToList();
        }

        private static string Clean(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            string output = input;

            // Удаляем все управляющие символы и ^
            output = Regex.Replace(output, @"[\b\x08\x07\x1B^]+", "");

            // Удаляем --More-- и всё после него до следующей полезной строки
            output = Regex.Replace(output, @"--More--.*?(?=epon|\Z)", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Удаляем повторяющиеся команды
            output = Regex.Replace(output, @"show epon optical-transceiver-diagnosis.*", "", RegexOptions.IgnoreCase);
            output = Regex.Replace(output, @"optical-transceiver-diagnosis interface epon.*", "", RegexOptions.IgnoreCase);

            // Удаляем промпт OLT
            output = Regex.Replace(output, @"^.*OLT_.*#$", "", RegexOptions.Multiline);

            // Удаляем строки с заголовками, которые не нужны
            output = Regex.Replace(output, @"interface Temperature.*", "", RegexOptions.IgnoreCase);
            output = Regex.Replace(output, @"-----------.*", "");

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(l => l.Trim())
                              .Where(l => !string.IsNullOrWhiteSpace(l) &&
                                          l.Contains("epon") && 
                                          Regex.IsMatch(l, @"[-\d.]+")) // оставляем только строки с номером ONU и значением
                              .ToList();

            return string.Join("\n", lines);
        }
    }
}