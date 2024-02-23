using Microsoft.CodeAnalysis;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace TestServer.Server
{
    public class Block
    {
        private static SHA256 sha256Hash = SHA256.Create();
        public string Hash { get; set; }
        public string PreviousHash { get; set; }
        public byte [] Data { get; set; }

        public Block() { }

        public Block(byte[] bytes) 
        {
            try { 
                Block block = Block.Desserialize(bytes);
                Data = block.Data;
                Hash = block.Hash;
                PreviousHash = block.PreviousHash;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public Block(byte[] dataIn, String previousHashIn)
        {
            try { 
                byte[] compressedDataIn = CompressData(dataIn);

                Data = new byte[compressedDataIn.Length];
                Array.Copy(compressedDataIn, 0, Data, 0, compressedDataIn.Length);

                PreviousHash = previousHashIn;

                Hash = CalculateHash();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public Block(byte[] dataIn,
                     String hashIn,
                     String previousHashIn)
        {    
            try { 
                byte[] compressedDataIn = CompressData(dataIn);

                Data = new byte[compressedDataIn.Length];
                Array.Copy(compressedDataIn, 0, Data, 0, compressedDataIn.Length);

                Hash = hashIn;
                PreviousHash = previousHashIn;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
        
        private byte[] CompressData(byte[] byteArray)
        {
            try { 
                MemoryStream strm = new MemoryStream();
                GZipStream GZipStrem = new GZipStream(strm, CompressionMode.Compress, true);
                GZipStrem.Write(byteArray, 0, byteArray.Length);
                GZipStrem.Flush();
                strm.Flush();
                byte[] ByteArrayToreturn = strm.GetBuffer();
                GZipStrem.Close();
                strm.Close();
                return ByteArrayToreturn;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private byte[] DecompressData(byte[] byteArray)
        {
            try { 
                MemoryStream strm = new MemoryStream(byteArray);
                GZipStream GZipStrem = new GZipStream(strm, CompressionMode.Decompress, true);
                List<byte> ByteListUncompressedData = new List<byte>();

                int bytesRead = GZipStrem.ReadByte();
                while (bytesRead != -1)
                {
                    ByteListUncompressedData.Add((byte)bytesRead);
                    bytesRead = GZipStrem.ReadByte();
                }
                GZipStrem.Flush();
                strm.Flush();
                GZipStrem.Close();
                strm.Close();
                return ByteListUncompressedData.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public byte[] GetData()
        {
            try { 
                return DecompressData(Data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private string CalculateHash()
        {        
            try { 

                // Convert the input string to a byte array and compute the hash.
                string input = PreviousHash + Encoding.UTF8.GetString(Data);

                byte[] hashBytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                var sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data
                // and format each one as a hexadecimal string.
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sBuilder.Append(hashBytes[i].ToString("x2"));
                }

                // Return the hexadecimal string.
                return sBuilder.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public byte[] Serialize()
        {
            try { 
                using (MemoryStream m = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(m))
                    {
                        writer.Write(Data.Length);
                        writer.Write(Data);
                        writer.Write(Hash);
                        writer.Write(PreviousHash);
                        writer.Flush();
                        writer.Close();
                    }

                    return m.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public static Block Desserialize(byte[] data)
        {
            try { 
                Block result = new Block();
                using (MemoryStream m = new MemoryStream(data))
                {
                    using (BinaryReader reader = new BinaryReader(m))
                    {
                        int dataSize = reader.ReadInt32();
                        result.Data = reader.ReadBytes(dataSize);
                        result.Hash = reader.ReadString();
                        result.PreviousHash = reader.ReadString();
                        reader.Close();
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    }
}
