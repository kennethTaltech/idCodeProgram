using System.Text.RegularExpressions;
using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Globalization;

namespace Exc1
{
    class IdDecoder
    {


        static void Main(string[] args)
        {

            PrintIdCodeInfo("34501234215");
            string filePath = "Data/idCodes.txt";
            ReadCodesFromFile(filePath);

        }

        static void ReadCodesFromFile(string filePath) {
            string line = string.Empty;
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        Console.WriteLine("\n======== New code: ========\n");
                        PrintIdCodeInfo(line);
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Error: The file '{filePath}' was not found. {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading the file: {ex.Message}");
            }
        } 

        static void PrintIdCodeInfo(string idCode)
        {
            string gender = string.Empty;
            string birthdate = string.Empty;
            string birthplaceAndNum = string.Empty;
            int checksum = 0;

            // Get values using the helper method
            birthdate = RetrieveValue(GetBirthdate, idCode) ?? "Unknown";
            birthplaceAndNum = RetrieveValue(GetHospital, idCode) ?? "Unknown";
            gender = RetrieveValue(GetGender, idCode) ?? "Unknown";
            checksum = RetrieveValue(GetChecksum, idCode);
            int.TryParse(idCode[10].ToString(), out int encodedChecksum);

            try
            {
                // Console log the results
                Console.WriteLine($"Decoding ID code {idCode}\nBirthdate: {birthdate}\nGender: {gender}\nBirthplace: {birthplaceAndNum}\nCalculated checksum: {checksum}\nEncoded checksum: {encodedChecksum}\nChecksums match: {checksum == encodedChecksum}");
            }
            catch (Exception ex)
            {
                // Set the console text color to red
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: ID code is corrupt");
                Console.WriteLine($"Error: {ex.Message}");
                // Reset the console text color to its default
                Console.ResetColor();
            }
                
        }

        // Helper method to retrieve values with error handling
        static T RetrieveValue<T>(Func<string, T> retrievalFunc, string idCode)
        {
            try
            {
                return retrievalFunc(idCode);
            }
            catch (Exception ex)
            {
                // Set the console text color to red
                Console.ForegroundColor = ConsoleColor.Red;
                // Log the exception to the console
                Console.WriteLine($"Error: {ex.Message}");
                // Reset the console text color to its default
                Console.ResetColor();
                return default; // Return default value for the type T

            }
        }

        static bool IsValidIdCode(string idCode)
        {
            // Check if the idCode is numeric and has the correct length
            bool isNumeric = Regex.IsMatch(idCode, @"^\d+$");

            // Validate that idCode length is 11 and all characters are numeric
            return isNumeric && idCode.Length == 11;
        }

        static string GetBirthdate(string idCode) {

            int birthYear = 0;
            string birthMonth = idCode.Substring(3, 2);
            string birthDay = idCode.Substring(5, 2);
            int month, day;

            // Get birth century using switch expression
            birthYear += idCode[0] switch
            {
                '1' or '2' => 1800,
                '3' or '4' => 1900,
                '5' or '6' => 2000,
                '7' or '8' => 2100,
                _ => throw new Exception("Invalid ID code; first digit must be between 1 and 8")
            };

            if (!int.TryParse(idCode.Substring(1, 2), out int birthSuffix))
            {
                throw new Exception("Invalid birth year in ID code");
            }
            birthYear += birthSuffix;

            if (!int.TryParse(birthMonth, out month) || month < 1 || month > 12)
            {
                throw new Exception("Invalid birth month in ID code");
            }

            if (!int.TryParse(birthDay, out day) || day < 1 || day > 31)
            {
                throw new Exception("Invalid birth day in ID code");
            }

            // Combine day, month, and year into a string for validation
            string dateText = $"{birthDay}.{birthMonth}.{birthYear}";
            string format = "dd.MM.yyyy"; // Define the date format

            // Validate the date using DateTime.ParseExact
            try
            {
                DateTime parsedDate = DateTime.ParseExact(dateText, format, CultureInfo.CurrentCulture);
                return parsedDate.ToString("dd.MM.yyyy"); // Format the output as desired
            }
            catch (FormatException)
            {
                throw new Exception($"{dateText} is an invalid date value");
            }

        }

        static string GetGender(string idCode)
        {
            // Check if the first character is a digit
            if (!char.IsDigit(idCode[0]))
            {
                throw new Exception("The first character of ID code must be a digit.");
            }

            int.TryParse(idCode[0].ToString(), out int firstDigit);
            return firstDigit % 2 == 0 ? "female" : "male";
        }
        static int GetChecksum(string idCode)
        {
            // This whole function is based on the wikipedia article.

            // Ensure the ID code is exactly 11 digits
            if (idCode.Length != 11 || !idCode.All(char.IsDigit))
                throw new ArgumentException("ID code must be exactly 11 digits long.");

            // Convert the first 10 characters to an array of integers
            int[] digits = idCode.Take(10).Select(c => c - '0').ToArray();

            // First set of weights used for checksum calculation
            int[] weights1 = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 1 };

            // Second set of weights used if the result of the first check is 10
            int[] weights2 = { 3, 4, 5, 6, 7, 8, 9, 1, 2, 3 };

            // Calculate the sum of products for the first set of weights
            int sum1 = digits.Zip(weights1, (d, w) => d * w).Sum();

            // Modulo 11 to get the potential checksum digit
            int checksum = sum1 % 11;

            // If checksum is less than 10, return it
            if (checksum < 10) return checksum;

            // If checksum is 10, use the second set of weights
            int sum2 = digits.Zip(weights2, (d, w) => d * w).Sum();

            // Return the result of the second check or 0 if it's still 10
            return sum2 % 11 < 10 ? sum2 % 11 : 0;
        }

        static string GetHospital(string idCode)
        {
            // Ensure the ID code is exactly 11 digits
            if (idCode.Length != 11 || !idCode.All(char.IsDigit))
            {
                throw new ArgumentException("ID code must be exactly 11 digits long.");
            }

            // Extract the 8th, 9th, and 10th digits as a string
            string hospitalCodeStr = idCode.Substring(7, 3);
            int hospitalCode = int.Parse(hospitalCodeStr); // Convert to integer for easier comparison

            // Define hospital mapping using ranges in a dictionary
            // These values are taken from wikipedia
            var hospitalRanges = new Dictionary<string, (int Min, int Max)>
        {
            { "Kuressaare haigla", (1, 10) },
            { "Tartu Ülikooli Naistekliinik", (11, 19) },
            { "Ida-Tallinna keskhaigla, Pelgulinna sünnitusmaja (Tallinn)", (21, 150) },
            { "Keila haigla", (151, 160) },
            { "Rapla haigla, Loksa haigla, Hiiumaa haigla (Kärdla)", (161, 220) },
            { "Ida-Viru keskhaigla (Kohtla-Järve, endine Jõhvi)", (221, 270) },
            { "Maarjamõisa kliinikum (Tartu), Jõgeva haigla", (271, 370) },
            { "Narva haigla", (371, 420) },
            { "Pärnu haigla", (421, 470) },
            { "Haapsalu haigla", (471, 490) },
            { "Järvamaa haigla (Paide)", (491, 520) },
            { "Rakvere haigla, Tapa haigla", (521, 570) },
            { "Valga haigla", (571, 600) },
            { "Viljandi haigla", (601, 650) },
            { "Lõuna-Eesti haigla (Võru), Põlva haigla", (651, 700) }
        };

            // Loop through the dictionary to find the corresponding hospital
            foreach (var entry in hospitalRanges)
            {
                if (hospitalCode >= entry.Value.Min && hospitalCode <= entry.Value.Max)
                {
                    // Calculate the birth number for that hospital
                    int birthNumber = hospitalCode - entry.Value.Min + 1; // Get the "nth" birth
                    return $"{entry.Key}, birth number {birthNumber}";
                }
            }

            throw new Exception("Unknown hospital code.");
        }
    }
}


