using System;
using System.Linq;

namespace Messenger
{
    static class Kuznechik
    {

        static byte[][] iterationConst = new byte[32][]; // массив итерационных констант
        static byte[][] iterationKey = new byte[10][]; // массив итерационных ключей
        static byte[] LVec = new byte[] // маccив
        {148, 32, 133, 16, 194, 192, 1, 251, 1, 192, 194, 16, 133, 32, 148, 1};

        static readonly byte[] Pi = new byte[256] // таблица для прямого нелинейного преобразования
        {
            252, 238, 221, 17, 207, 110, 49, 22, 251, 196, 250, 218, 35, 197, 4, 77,
            233, 119, 240, 219, 147, 46, 153, 186, 23, 54, 241, 187, 20, 205, 95, 193,
            249, 24, 101, 90, 226, 92, 239, 33, 129, 28, 60, 66, 139, 1, 142, 79,
            5, 132, 2, 174, 227, 106, 143, 160, 6, 11, 237, 152, 127, 212, 211, 31,
            235, 52, 44, 81, 234, 200, 72, 171, 242, 42, 104, 162, 253, 58, 206, 204,
            181, 112, 14, 86, 8, 12, 118, 18, 191, 114, 19, 71, 156, 183, 93, 135,
            21, 161, 150, 41, 16, 123, 154, 199, 243, 145, 120, 111, 157, 158, 178, 177,
            50, 117, 25, 61, 255, 53, 138, 126, 109, 84, 198, 128, 195, 189, 13, 87,
            223, 245, 36, 169, 62, 168, 67, 201, 215, 121, 214, 246, 124, 34, 185, 3,
            224, 15, 236, 222, 122, 148, 176, 188, 220, 232, 40, 80, 78, 51, 10, 74,
            167, 151, 96, 115, 30, 0, 98, 68, 26, 184, 56, 130, 100, 159, 38, 65,
            173, 69, 70, 146, 39, 94, 85, 47, 140, 163, 165, 125, 105, 213, 149, 59,
            7, 88, 179, 64, 134, 172, 29, 247, 48, 55, 107, 228, 136, 217, 231, 137,
            225, 27, 131, 73, 76, 63, 248, 254, 141, 83, 170, 144, 202, 216, 133, 97,
            32, 113, 103, 164, 45, 43, 9, 91, 203, 155, 37, 208, 190, 229, 108, 82,
            89, 166, 116, 210, 230, 244, 180, 192, 209, 102, 175, 194, 57, 75, 99, 182
        };

        static readonly byte[] Pi_Reverse = new byte[256] // таблица для обратного нелинейного преобразования
        {
            165, 45, 50, 143, 14, 48, 56, 192, 84, 230, 158, 57, 85, 126, 82, 145,
            100, 3, 87, 90, 28, 96, 7, 24, 33, 114, 168, 209, 41, 198, 164, 63,
            224, 39, 141, 12, 130, 234, 174, 180, 154, 99, 73, 229, 66, 228, 21, 183,
            200, 6, 112, 157, 65, 117, 25, 201, 170, 252, 77, 191, 42, 115, 132, 213,
            195, 175, 43, 134, 167, 177, 178, 91, 70, 211, 159, 253, 212, 15, 156, 47,
            155, 67, 239, 217, 121, 182, 83, 127, 193, 240, 35, 231, 37, 94, 181, 30,
            162, 223, 166, 254, 172, 34, 249, 226, 74, 188, 53, 202, 238, 120, 5, 107,
            81, 225, 89, 163, 242, 113, 86, 17, 106, 137, 148, 101, 140, 187, 119, 60,
            123, 40, 171, 210, 49, 222, 196, 95, 204, 207, 118, 44, 184, 216, 46, 54,
            219, 105, 179, 20, 149, 190, 98, 161, 59, 22, 102, 233, 92, 108, 109, 173,
            55, 97, 75, 185, 227, 186, 241, 160, 133, 131, 218, 71, 197, 176, 51, 250,
            150, 111, 110, 194, 246, 80, 255, 93, 169, 142, 23, 27, 151, 125, 236, 88,
            247, 31, 251, 124, 9, 13, 122, 103, 69, 135, 220, 232, 79, 29, 78, 4,
            235, 248, 243, 62, 61, 189, 138, 136, 221, 205, 11, 19, 152, 2, 147, 128,
            144, 208, 36, 52, 203, 237, 244, 206, 153, 16, 68, 64, 146, 58, 1, 38,
            18, 26, 72, 104, 245, 129, 139, 199, 214, 32, 10, 8, 0, 76, 215, 116
        };

        static private byte[] LinearTransformation(byte[] data) //Линейное преобрзование
        {
            for (int j = 0; j < 16; j++)
            {
                byte a_15 = 0;
                for (int i = 0; i <= 15; i++)
                    a_15 ^= MultInGoalaField(data[i], LVec[i]); //Перемножение в поле Галуа
                for (int i = 15; i > 0; i--)
                    data[i] = data[i - 1]; //Переносим байты к старшему разряду
                data[0] = a_15;
            }
            return data;
        }
        /* Обратное линейное L преобразование */
        static private byte[] ReversLinearTransformation(byte[] data)
        {
            for (int j = 0; j < 16; j++)
            {
                byte lastByte = data[0];
                for (int i = 0; i < 15; i++)
                    data[i] = data[i + 1];
                data[15] = 0;
                for (int i = 15; i >= 0; i--)
                    lastByte ^= MultInGoalaField(data[i], LVec[i]);
                data[15] = lastByte;
            }
            return data;
        }

        /* Нелинейное S преобразование */
        static private byte[] NonlinearTransformation(byte[] input, bool flag)
        {
            byte[] output = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                if (flag == false)
                    output[i] = Pi[input[i]];
                else
                    output[i] = Pi_Reverse[input[i]];
            }
            return output;
        }

        /* Линейное L преобразование */
        static private byte MultInGoalaField(byte a, byte b)
        {
            byte p = 0;
            byte hi_bit;
            for (int i = 0; i < 8 && a != 0 && b != 0; i++)
            {
                if ((b & 1) != 0) //Добавляем по модулю 2 только те значения, на позиции которых стоит 1
                    p ^= a;
                hi_bit = (byte)(a & 128); //В байте a хранится значение из таблицы степеней двойки для поля GF(2^8)
                a <<= 1;
                if (hi_bit != 0)
                    a ^= 195; // Неприводимый полином x^8 + x^7 + x^6 + x + 1
                b >>= 1;
            }
            return p;
        }

        /* Сложение_по_модулю_2 */
        static private byte[] Xor(byte[] input1, byte[] input2) 
        {
            byte[] output = new byte[16];
            for (int i = 0; i < 16; i++)           
                output[i] = Convert.ToByte(input1[i] ^ input2[i]);           
            return output;
        }

        /* Функция F сети Фейстиля */
        static private byte[] F(byte[] key, byte[] roundC)
        {
            key = Xor(key, roundC);
            key = NonlinearTransformation(key, false);
            return LinearTransformation(key);
        }

        /* Изменение размера ключа до 32 символов */
        static private byte[] KeyLenghtResizeTo32Byte(byte[] key) 
        {
            if (key.Length < 32)
            {
                Console.WriteLine("Колюч дополнен до 32 символов");
                Array.Resize(ref key, 32);
                int j = 0;
                for (int i = key.Length; i < 32; i++, j++)
                    key[i] = key[j];
            }
            else if (key.Length > 32)
                Console.WriteLine("Ключ обрезан до 32 символов");
            return key.Take(32).ToArray();
        }

        /* Генерация ключей */
        static private void KeyGeneration(byte[] masterKey)
        {
            /* Генерация_раундовых_констант */
            byte[] iterNum;
            for (int i = 0; i < 32; i++)
            {
                iterNum = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (byte)(i + 1) };
                iterationConst[i] = LinearTransformation(iterNum); //Генерация констант по линейному преобразования
            }

            /* Генерация первых 2ух ключей */
            byte[] A = masterKey.Take(16).ToArray();
            byte[] B = masterKey.Skip(16).ToArray();
            byte[] tempKey = new byte[16];
            for (int i = 0; i < iterationKey.Length; i++)
            {
                iterationKey[i] = new byte[16];
            }
            A.CopyTo(iterationKey[0], 0);
            B.CopyTo(iterationKey[1], 0);

            /* Генерация остальных ключей */
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Array.Copy(A, tempKey, 16);
                    A = F(A, iterationConst[i * 8 + j]);
                    A = Xor(A, B);
                    Array.Copy(tempKey, B, 16);
                }
                A.CopyTo(iterationKey[2 * i + 2], 0);
                B.CopyTo(iterationKey[2 * i + 3], 0);
            }
        }

        /* Зашифрование текста */
        static public byte[] Encript(byte[] text, byte[] masterKey)
        {
            masterKey = KeyLenghtResizeTo32Byte(masterKey);
            KeyGeneration(masterKey);
            int NumOfBlocks = (int)Math.Ceiling(text.Length / 16d); // Определение кол-ва блоков по 16 байт
            Array.Resize(ref text, NumOfBlocks * 16);
            byte[] encryptedText = new byte[NumOfBlocks * 16]; // Массив для хранения зашифрованных байтов
            byte[] block = new byte[16];

            for (int i = 0; i < NumOfBlocks; i++) // Операция зашифровки
            {
                Array.Copy(text, i * 16, block, 0, 16);
                for (int j = 0; j < 9; j++)
                {
                    block = Xor(block, iterationKey[j]);
                    block = NonlinearTransformation(block, false);
                    block = LinearTransformation(block);
                }
                block = Xor(block, iterationKey[9]);

                Array.Copy(block, 0, encryptedText, i * 16, 16); //Запись результата шифрования блока
            }
            return encryptedText;
        }

        /* Расшифрование текста */
        static public byte[] Decrypt(byte[] text, byte[] masterKey)
        {
            masterKey = KeyLenghtResizeTo32Byte(masterKey);
            KeyGeneration(masterKey);
            int NumOfBlocks = (int)Math.Ceiling(text.Length / 16d); // Определение кол-ва блоков по 16 байт
            byte[] decryptedText = new byte[NumOfBlocks * 16]; // Массив для хранения зашифрованных байтов
            byte[] block = new byte[16];

            for (int i = 0; i < NumOfBlocks; i++)
            {
                Array.Copy(text, i * 16, block, 0, 16);
                block = Xor(block, iterationKey[9]);
                for (int j = 8; j >= 0; j--)
                {
                    block = ReversLinearTransformation(block);
                    block = NonlinearTransformation(block, true);
                    block = Xor(block, iterationKey[j]);
                }
                Array.Copy(block, 0, decryptedText, i * 16, 16);
            }
            return decryptedText;
        }               
    }
}
