using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WebServerTask
{
    public static class FileIO
    {
        private static string fileName = "PoolController.dat";
        public static void ReadFile()
        {
            
            if (File.Exists(fileName))
                File.Create(fileName);
            try
            {
                File.ReadLines(fileName);
            }
            catch (Exception)
            {
                    
            }
        }

        public static void WriteFile()
        {
            if (File.Exists(fileName))
                File.Create(fileName);
            try
            {
                File.WriteAllText(fileName, "Test");
            }
            catch (Exception)
            {

            }
        }

    }
}
