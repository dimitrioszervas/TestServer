using System.Data;

namespace TestServer.Server
{
    public class Blockchain
    {
        private const string HEADER = "BLOCKCHAIN HEADER";
        public const string INDEX_FILE = "index";

        private string blockchainFilename;
        private string indexFilename;
        private string id;

        public Blockchain(string filenameIn)
        {
        
            try { 

                this.blockchainFilename = filenameIn;
                this.indexFilename = filenameIn + INDEX_FILE;

                using (FileStream fs = File.OpenRead(this.blockchainFilename))
                {
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        string header = reader.ReadString();
                        if (!header.Equals(HEADER))
                        {
                            Console.WriteLine("NOT a valid BLOCKCHAIN file");
                            reader.Close();
                            return;
                        }
                        this.id = reader.ReadString();
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public Blockchain(string filenameIn, string idIn)
        {
            try { 
                this.blockchainFilename = filenameIn;
                this.indexFilename = filenameIn + INDEX_FILE;
                this.id = idIn;
          
                using (FileStream fs = File.OpenWrite(this.blockchainFilename)) 
                {
                    using (BinaryWriter w = new BinaryWriter(fs))
                    {                    
                        w.Write(HEADER);
                        w.Write(id.ToString());
                        w.Flush();
                        w.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public int GetSize()
        {
            try {             
                FileInfo fileInfo = new FileInfo(this.indexFilename);
        
                if (!fileInfo.Exists)
                {
                    return 0;
                }

                return (int)(fileInfo.Length / sizeof(long));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private long GetBlockOffsetFromIndexFile(long indexBlockOffset)
        {
            try { 

                long blockOffset;

                using (BinaryReader b = new BinaryReader(File.Open(this.indexFilename, FileMode.Open)))
                {
                    // Seek to our required position.
                    b.BaseStream.Seek(indexBlockOffset, SeekOrigin.Begin);

                    blockOffset = b.ReadInt64();

                    b.Close();
                }

                return blockOffset;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private Block GetBlockFromBlockchainFile(long blockOffset)
        {
            try { 
                byte[] bytes;
                using (BinaryReader b = new BinaryReader(File.Open(this.blockchainFilename, FileMode.Open)))
                {
                    b.BaseStream.Seek(blockOffset, SeekOrigin.Begin);

                    int bytesSize = b.ReadInt32();

                    bytes = b.ReadBytes(bytesSize);

                    b.Close();
                }

                return new Block(bytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public Block GetBlock(int blockNo)
        {
            try {
                lock (this)
                {
                    long blockOffsetFromIdexFile = this.GetBlockOffsetFromIndexFile(blockNo * sizeof(long));

                    if (blockOffsetFromIdexFile == 0)
                    {
                        return null;
                    }

                    return this.GetBlockFromBlockchainFile(blockOffsetFromIdexFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public Block GetLastBlock()
        {
            try {
                
                FileInfo fileInfo = new FileInfo(this.indexFilename);

                if (!fileInfo.Exists)
                {
                    return null;
                }

                long lastBlockOffsetfromIndexFile = fileInfo.Length - sizeof(long);

                long lastBlockOffset = this.GetBlockOffsetFromIndexFile(lastBlockOffsetfromIndexFile);

                if (lastBlockOffset == 0)
                {
                    return null;
                }

                return this.GetBlockFromBlockchainFile(lastBlockOffset);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private void Add(Block newBlock)
        {     
            try { 

                FileInfo fileInfo = new FileInfo(this.blockchainFilename);

                if (!fileInfo.Exists)
                {
                    return;
                }

                long filePos = fileInfo.Length;

                using (var fileStream = new FileStream(this.blockchainFilename, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    using (var bw = new BinaryWriter(fileStream))
                    {  
                        byte[] bytes = newBlock.Serialize();
                        bw.Write(bytes.Length);
                        bw.Write(bytes);
                        bw.Flush();
                        bw.Close();
                    }

                }

                // Add block offset position to index file /////////////////////////
                using (var fileStream = new FileStream(this.indexFilename, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    using (var bw = new BinaryWriter(fileStream))
                    {
                        bw.Write(filePos);
                        bw.Flush();
                        bw.Close();
                    }            
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    
        public void Add(byte[] data)
        {              
            try {
                lock (this)
                {
                    string lastBlockHash = "0";

                    Block lastBlock = this.GetLastBlock();

                    if (lastBlock != null)
                    {
                        lastBlockHash = lastBlock.Hash;
                    }

                    this.Add(new Block(data, lastBlockHash));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public string GetId()
        {
            return this.id;
        }

        public bool IsEmpty()
        {
            try {              
                return this.GetSize() == 0;               
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public string GetLastBlockHash()
        {
            try {
                             
                if (this.GetSize() == 0)
                {
                    return "0";
                }

                Block lastBlock = this.GetLastBlock();

                if (lastBlock == null)
                {
                    return "0";
                }

                return lastBlock.Hash;                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        public string GetFirstBlockHash()
        {
            try {
               
                if (this.GetSize() == 0)
                {
                    return "0";
                }

                Block firstBlock = this.GetBlock(1);

                if (firstBlock == null)
                {
                    return "0";
                }

                return firstBlock.Hash;
               
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }   
     
        public long GetLastBlockNo()
        {
            try {
                return this.GetSize() - 1;                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    }
}
