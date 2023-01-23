using System;
using System.Text;

namespace MyApp // Note: actual namespace depends on the project name.
{
    class Program
    {
        public static int Main()
        {
            string folder = "C:\\Reports\\ToLower\\"; // папка откуда брать + \\ чтобы туда зайти и сохранять файлы
            string[] filenames = Directory.GetFiles(folder);
            foreach (string filename in filenames)
            {
                string[] file = new string[2500];
                string newFileName = filename.Substring(0, filename.IndexOf(".txt")) + "Removed.txt";
                StreamReader sr = new StreamReader(filename);//C:\\reports\\ToLower.txt

                int rows = 0;
                while (sr.Peek() >= 0)
                {
                    file[rows] = sr.ReadLine().ToLower();
                    rows++;
                }
                writeToFile(newFileName, file, rows);
                sr.Close();
            }
            return 0;
        }
        async public static void writeToFile(string filename, string[] result, int rows)
        {
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            StreamWriter fileNew = new(filename);
            for (int i = 0; i <= rows; i++)
            {
                fileNew.WriteLine(result[i]);
            }
            fileNew.Close();
        }
    }
}

