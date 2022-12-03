using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace P3DMerge
{
    public static class lzr_compression
    {
        private const uint CompressedSignature = 0x5A443350;

        private static List<byte> DecompressBlock(BinaryReader br, uint size)
        {
            List<byte> output = new List<byte>((int)size);

            int written = 0;
            
            while (written < size)
            {
                byte code = br.ReadByte();

                if (code > 15)
                {
                    int matchLength = code & 15;
                    byte tmp;

                    if (matchLength == 0)
                    {
                        matchLength = 15;
                        tmp = br.ReadByte();
                        while (tmp == 0)
                        {
                            matchLength += 255;
                            tmp = br.ReadByte();
                        }
                        matchLength += tmp;
                    }

                    tmp = br.ReadByte();
                    int offset = (code >> 4) | tmp << 4;
                    int matchPos = written - offset;

                    int len = matchLength >> 2;
                    matchLength -= len << 2;

                    do
                    {
                        output.Add(output[matchPos]);
                        written++;
                        matchPos++;
                        output.Add(output[matchPos]);
                        written++;
                        matchPos++;
                        output.Add(output[matchPos]);
                        written++;
                        matchPos++;
                        output.Add(output[matchPos]);
                        written++;
                        matchPos++;

                        len--;
                    } while (len != 0);

                    while (matchLength != 0)
                    {
                        output.Add(output[matchPos]);
                        written++;
                        matchPos++;

                        matchLength--;
                    }
                }
                else
                {
                    byte runLength = code;

                    if (runLength == 0)
                    {
                        code = br.ReadByte();
                        while (code == 0)
                        {
                            runLength += 255;
                            code = br.ReadByte();
                        }
                        runLength += code;

                        output.AddRange(br.ReadBytes(15));
                        written += 15;
                    }

                    output.AddRange(br.ReadBytes(runLength));
                    written += runLength;
                }
            }

            return output;
        }

        private static List<byte> Decompress(BinaryReader file, uint UncompressedLength)
        {
            uint decompressedLength = 0;
            List<byte> output = new List<byte>();
            uint compressedLength;
            uint uncompressedBlock;
            long startPos;
            while (decompressedLength < UncompressedLength)
            {
                compressedLength = file.ReadUInt32();
                uncompressedBlock = file.ReadUInt32();
                startPos = file.BaseStream.Position;
                output.AddRange(DecompressBlock(file, uncompressedBlock));
                decompressedLength += uncompressedBlock;
                file.BaseStream.Seek(startPos + compressedLength, SeekOrigin.Begin);
            }
            return output;
        }

        public static byte[] DecompressFile(BinaryReader file)
        {
            uint identifier = file.ReadUInt32();
            if (identifier != CompressedSignature)
            {
                file.BaseStream.Seek(0, SeekOrigin.Begin);
                return file.ReadBytes((int)file.BaseStream.Length);
            }

            uint length = file.ReadUInt32();
            List<byte> decompressed = Decompress(file, length);
            return decompressed.ToArray();
        }
    }
}
