using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PuzReader
{
    public class ItemCell
    {
        public enum ItemCellType
        {
            Letter,
            Shade
        }
        public int x { get; set; }
        public int y { get; set; }
        public ItemCellType type { get; set; }
        public string value { get; set; }
        public string state { get; set; }
    }
    public class Clue
    {
        public enum ClueDir { 
            Horizontal,
            Vertical
        }
        public int numClue { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public int num { get; set; }
        public ClueDir dir { get; set; }
        public string value { get; set; }
    }
    public class PuzFile
    {
        public short checksum { get; set; }
        public string fileMagic { get; set; }
        public short cibChecksum { get; set; }
        public short maskedLowChecksum { get; set; }
        public short maskedHighChecksum { get; set; }
        public string version { get; set; }
        public string Reserved1C { get; set; }
        public short scrambledChecksum { get; set; }
        public string Reserved20 { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public short numberOfClues { get; set; }
        public short unknownBitmask { get; set; }
        public short scrambledTag { get; set; }
        public string solution { get; set; }
        public string state { get; set; }

        public string title { get; set; }
        public string author { get; set; }
        public string copyright { get; set; }
        public string notes { get; set; }

        public IList<ItemCell> items { get; set; }
        public IList<Clue> clues { get; set; }

        public PuzFile() { }
        public void start(string nomeFile)
        {
            string puzFilePath = String.Format("{0}/puz/{1}", Environment.CurrentDirectory, nomeFile);

            byte[] puz = File.ReadAllBytes(puzFilePath);

            //checksum
            byte[] bytes = puz.Skip(0).Take(0x2).ToArray();
            checksum = BitConverter.ToInt16(bytes, 0);

            //filemagic
            bytes = puz.Skip(0x02).Take(0xC).ToArray();
            fileMagic = Encoding.ASCII.GetString(bytes);//BitConverter.ToString(bytes);

            //CIB Checksum
            bytes = puz.Skip(0x0E).Take(0x2).ToArray();
            cibChecksum = BitConverter.ToInt16(bytes, 0);

            //Masked Low Checksums
            bytes = puz.Skip(0x10).Take(0x4).ToArray();
            maskedLowChecksum = BitConverter.ToInt16(bytes, 0);

            //Masked High Checksums
            bytes = puz.Skip(0x14).Take(0x4).ToArray();
            maskedHighChecksum = BitConverter.ToInt16(bytes, 0);

            //Version String(?)
            bytes = puz.Skip(0x18).Take(0x4).ToArray();
            version = BitConverter.ToString(bytes);

            //Reserved1C
            bytes = puz.Skip(0x1C).Take(0x2).ToArray();
            Reserved1C = BitConverter.ToString(bytes);

            //Scrambled Checksum
            bytes = puz.Skip(0x1C).Take(0x2).ToArray();
            scrambledChecksum = BitConverter.ToInt16(bytes, 0);

            //Reserved20
            bytes = puz.Skip(0x20).Take(0xC).ToArray();
            Reserved20 = BitConverter.ToString(bytes);

            //Width
            bytes = puz.Skip(0x2C).Take(0x1).ToArray();
            width = bytes[0];

            //Height
            bytes = puz.Skip(0x2D).Take(0x1).ToArray();
            height = bytes[0];

            //number of clues
            bytes = puz.Skip(0x2E).Take(0x2).ToArray();
            numberOfClues = BitConverter.ToInt16(bytes, 0);

            //Unknown Bitmask
            bytes = puz.Skip(0x30).Take(0x2).ToArray();
            unknownBitmask = BitConverter.ToInt16(bytes, 0);

            //Scrambled Tag
            bytes = puz.Skip(0x32).Take(0x2).ToArray();
            scrambledTag = BitConverter.ToInt16(bytes, 0);

            int cells = width * height;
            int solutionStart = 0x34;
            int stateStart = solutionStart + cells;

            //solution
            bytes = puz.Skip(solutionStart).Take(cells).ToArray();
            solution = Encoding.ASCII.GetString(bytes);

            //state
            bytes = puz.Skip(stateStart).Take(cells).ToArray();
            state = Encoding.ASCII.GetString(bytes);

            int stringStart = stateStart + cells;
            bytes = puz.Skip(stringStart).ToArray();

            Encoding iso_8859_1 = Encoding.GetEncoding("iso-8859-1");

            string strings = iso_8859_1.GetString(bytes);

            IList<string> cluesStr = new List<string>();
            byte[] tempBytes;
            string tempString;
            int j = 0;
            int bytesLength = bytes.Length;
            for (int i = 0; i < bytesLength; i++)
            {
                if (bytes[i] == 0)
                {
                    tempBytes = readTillPrevZero(i, bytes);
                    tempString = tempBytes != null ? iso_8859_1.GetString(tempBytes) : null;
                    if (j == 0) title = tempString;
                    if (j == 1) author = tempString;
                    if (j == 2) copyright = tempString;
                    if (j > 2) 
                    {
                        if (i < bytesLength - 1)
                        {
                            cluesStr.Add(tempString);
                        }
                        else
                        {
                            notes = tempString;
                        }
                        //Console.WriteLine(tempBytes != null ? iso_8859_1.GetString(tempBytes) : String.Empty);
                    }
                    
                    j++;
                }
            }

            clues = new List<Clue>();
            items = new List<ItemCell>();

            int cellNumber = 1;
            int clueNumber = 0;
            bool flagAssignedNumber = false;
            bool blackCell = false;
            bool needsAcrossNumber = false;
            bool needsDownNumber = false;
            string letter;
            string clue;
            //Parsing della solution
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    blackCell = isBlackCell(x, y);
                    letter = getLetter(x, y);
                    items.Add(new ItemCell() { x = x, y = y, type = blackCell ? ItemCell.ItemCellType.Shade : ItemCell.ItemCellType.Letter, value =  letter});

                    Console.WriteLine(String.Format("X:{0}, Y:{1}, Black:{2}, Letter:{3}", x, y, blackCell, letter));

                    ///Console.WriteLine(String.Format("X:{0}, Y:{1}, Black:{2}", x, y, blackCell));
                    if (blackCell) continue;

                    flagAssignedNumber = false;
                    needsAcrossNumber = cellNeedsAcrossNumber(x, y);
                    //Console.WriteLine(String.Format("X:{0}, Y:{1}, Needs across number:{2}", x, y, needsAcrossNumber));
                    needsDownNumber = cellNeedsDownNumber(x, y);
                    //Console.WriteLine(String.Format("X:{0}, Y:{1}, Needs down number:{2}", x, y, needsDownNumber));

                    if (needsAcrossNumber)
                    {
                        clue = cluesStr.ElementAt(clueNumber);
                        //Console.WriteLine(String.Format("X:{0}, Y:{1}, Clue number:{2}", x, y, cellNumber));
                        clues.Add(new Clue() { dir = Clue.ClueDir.Horizontal, numClue = cellNumber, num = getNumberLetter(x, y, Clue.ClueDir.Horizontal), x = x, y = y, value = clue });
                        Console.WriteLine(String.Format("X:{0}, Y:{1}, Clue number:{2}, Position:{3}, Clue:{4}", x, y, cellNumber, "Hor", clue));
                        flagAssignedNumber = true;
                        clueNumber++;
                    }
                    if (needsDownNumber)
                    {
                        clue = cluesStr.ElementAt(clueNumber);
                        //Console.WriteLine(String.Format("X:{0}, Y:{1}, Clue number:{2}", x, y, cellNumber));
                        clues.Add(new Clue() { dir = Clue.ClueDir.Vertical, numClue = cellNumber, num = getNumberLetter(x, y, Clue.ClueDir.Vertical), x = x, y = y, value = clue });
                        Console.WriteLine(String.Format("X:{0}, Y:{1}, Clue number:{2}, Position:{3}, Clue:{4}", x, y, cellNumber, "Ver", clue));
                        flagAssignedNumber = true;
                        clueNumber++;
                    }
                    if (flagAssignedNumber) cellNumber = cellNumber + 1;
                }
            }

        }
        int getNumberLetter(int x, int y, Clue.ClueDir dir)
        {
            int v = 0;
            if (dir == Clue.ClueDir.Horizontal)
            {
                while (x < width && !isBlackCell(x, y))
                {
                    v++;
                    x++;
                }
            }
            if (dir == Clue.ClueDir.Vertical)
            {
                while (y < height && !isBlackCell(x, y))
                {
                    v++;
                    y++;
                }
            }
            return v;
        }
        byte[] readTillPrevZero(int i, byte[] bytes)
        {
            if (i == 0) return null;
            IList<byte> tempBytes = new List<byte>();
            int j = i - 1;
            while (j >= 0 && bytes[j] != 0)
            {
                tempBytes.Add(bytes[j]);
                j--;
            }
            return tempBytes.Reverse().ToArray();
        }

        string getLetter(int x, int y) 
        {
            int count = width * y + x;
            return (count >= 0 && !isBlackCell(x, y)) ? solution.Substring(count, 1) : null;
        }
        bool isBlackCell(int x, int y)
        {
            int count = width * y + x;
            return count >= 0 ? solution.Substring(count, 1) == "." : false;
        }
        bool cellNeedsAcrossNumber(int x, int y)
        {
            if (x == 0 || isBlackCell(x - 1, y))
                if (x + 1 < width && !isBlackCell(x + 1, y))
                    return true;

            return false;
        }
        bool cellNeedsDownNumber(int x, int y)
        {
            if (y == 0 || isBlackCell(x, y - 1))
                if (y + 1 < height && !isBlackCell(x,  y + 1))
                    return true;

            return false;
        }
    }
}
