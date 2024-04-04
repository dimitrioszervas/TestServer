﻿using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
using System.Text;

namespace TestServer.Server
{

    public static class CryptoUtils
    {
        public class ECDHKeyPair
        {
            public byte[] PublicKey { get; set; }
            public byte[] PrivateKey { get; set; }
        }

        private enum KeyType
        {
            SIGN,
            ENCRYPT
        }

        public const string OWNER_CODE = "1234";

        public const int NUM_SIGNS_OR_ENCRYPTS = Servers.NUM_SERVERS + 1;

        public const int KEY_SIZE_32 = 32;

        public const int TAG_SIZE = 16;
        public const int IV_SIZE = 12;

        public const int SRC_SIZE_8 = 8;

        public static byte[] Decrypt(byte[] cipherData, byte[] key, byte[] nonceIn)
        {

            // get raw cngPrivateKeyBlob spans
            var encryptedData = cipherData.AsSpan();

            var tagSizeBytes = 16; // 128 bit encryption / 8 bit = 16 cngPrivateKeyBlob           

            // ciphertext size is whole data - nonce - tag
            var cipherSize = encryptedData.Length - tagSizeBytes;

            // extract nonce (nonce) 12 cngPrivateKeyBlob prefix          
            byte[] nonce = new byte[12];
            Array.Copy(nonceIn, nonce, 8);

            // followed by the real ciphertext
            var cipherBytes = encryptedData.Slice(0, cipherSize);

            // followed by the tag (trailer)
            var tagStart = cipherSize;
            var tag = encryptedData.Slice(tagStart);

            // now that we have all the parts, the decryption
            Span<byte> plainBytes = cipherSize < 1024
                ? stackalloc byte[cipherSize]
                : new byte[cipherSize];
            using var aes = new AesGcm(key);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
            return plainBytes.ToArray();
        }

        /*

        public static byte[] Decrypt(byte[] encryptedData, byte[] cngPublicKey, byte[] nonceIn)
        {
            var ciphertext = encryptedData[0..^16];
            var tag = encryptedData[^16..];
            byte[] decrytedBytes = new byte[ciphertext.Length];
            try
            {
                var aes = new AesGcm(cngPublicKey);

                byte[] nonce = new byte[12];
                Array.Copy(nonceIn, nonce, 8);

                aes.Decrypt(nonce, ciphertext, tag, decrytedBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            return decrytedBytes;
        }
        */

        public static byte[] Decrypt(byte[] encryptedData, byte[] key, byte[] src, byte[] tag)
        {
            var ciphertext = encryptedData;// encryptedData[0..^16];
                                           //var tag = new byte[16];// encryptedData[^16..];
            byte[] decrytedBytes = new byte[ciphertext.Length];
            try
            {
                var aes = new AesGcm(key, TAG_SIZE);

                byte[] iv = new byte[IV_SIZE];
                Array.Copy(src, iv, 8);

                aes.Decrypt(iv, ciphertext, tag, decrytedBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            return decrytedBytes;
        }

        public static void Encrypt(byte[] plainBytes, byte[] key, byte[] src, ref byte[] ciphertext, ref byte[] tag)
        {
            var aes = new AesGcm(key, TAG_SIZE);

            byte[] iv = new byte[IV_SIZE];
            Array.Copy(src, iv, 8);

            aes.Encrypt(iv, plainBytes, ciphertext, tag);
        }

        public static string ByteArrayToStringDebug(byte[] bytes)
        {
            var sb = new StringBuilder("[");
            sb.Append(string.Join(", ", bytes));
            sb.Append("]");
            return sb.ToString();
        }

        public static byte[] DeriveHKDFKey(byte[] ikm, int outputLength, byte[] salt, byte[] info)
        {
            byte[] key = HKDF.DeriveKey(hashAlgorithmName: HashAlgorithmName.SHA256,
                                       ikm: ikm,
                                       outputLength: outputLength,
                                       salt: salt,
                                       info: info);
            return key;
        }

        public static byte[] DeriveHKDF32Key(byte[] ikm, byte[] salt, byte[] info)
        {
            byte[] key = DeriveHKDFKey(ikm: ikm,
                                       outputLength: 32,
                                       salt: salt,
                                       info: info);
            return key;
        }

        public static byte[] DeriveHKDF8Key(byte[] ikm, byte[] salt, byte[] info)
        {
            byte[] key = DeriveHKDFKey(ikm: ikm,
                                       outputLength: 8,
                                       salt: salt,
                                       info: info);
            return key;
        }

        public static byte[] ConvertInt32ToByteArray(int I32)
        {
            return BitConverter.GetBytes(I32);
        }

        public static byte[] ConvertRawECDHPublicKeyToCngKeyBlob(byte[] rawECDHPublicKey)
        {
            // For ECDH instead of ECDSA, change 0x53 to 0x4B.
            // ECCPublicKeyBlob is formatted(for P256) as follows
            // [KEY TYPE(4 bytes)][KEY LENGTH(4 bytes)][PUBLIC KEY(64 bytes)]
            byte[] keyType = new byte[] { 0x45, 0x43, 0x4B, 0x31 };
            byte[] keyLength = { 0x20, 0, 0, 0 };
            //byte[] keyLength = ConvertInt32ToByteArray((rawECDHPublicKey.Length - 1) / 2);

            byte[] key = new byte[rawECDHPublicKey.Length - 1];
            for (int i = 1; i < rawECDHPublicKey.Length; i++)
            {
                key[i - 1] = rawECDHPublicKey[i];
            }

            var cngKeyBlob = keyType.Concat(keyLength).Concat(key).ToArray();

            return cngKeyBlob;
        }

        public static byte[] ConverCngKeyBlobToRaw(byte[] CngKeyBlob)
        {
            byte[] key = new byte[CngKeyBlob.Length - 7];
            // PUBLIC KEY is the uncompressed format minus the
            // leading byte (which is always 04 to signify an
            // uncompressed key in other libraries)
            key[0] = 4;
            for (int i = 8; i < CngKeyBlob.Length; i++)
            {
                key[i + 1 - 8] = CngKeyBlob[i];
            }
            return key;
        }

        public static byte[] ECDHDerive(byte[] cngPrivateKeyBlob, byte[] publicKeyRaw)
        {
            CngKey cngPrivateKey = CngKey.Import(cngPrivateKeyBlob, CngKeyBlobFormat.EccPrivateBlob);

            byte[] cngPublicKeyBlob = ConvertRawECDHPublicKeyToCngKeyBlob(publicKeyRaw);
            var cngPublicKey = CngKey.Import(cngPublicKeyBlob, CngKeyBlobFormat.EccPublicBlob);

            using (ECDiffieHellmanCng ecDiffieHellmanCng = new ECDiffieHellmanCng(cngPrivateKey))
            {
                ecDiffieHellmanCng.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                ecDiffieHellmanCng.HashAlgorithm = CngAlgorithm.Sha256;

                byte[] derivedECDHKey = ecDiffieHellmanCng.DeriveKeyMaterial(cngPublicKey);

                return derivedECDHKey;
            }
        }


        private static List<byte[]> GenerateNKeys(int n, byte[] src, KeyType type, byte[] baseKey)
        {
            List<byte[]> keys = new List<byte[]>();
            byte[] salt, info;

            for (int i = 0; i <= n; i++)
            {
                if (type == KeyType.SIGN)
                {
                    salt = src; // salt needed to generate keys
                    info = Encoding.UTF8.GetBytes("SIGNS" + i);
                }
                else
                {

                    salt = src; // salt needed to generate keys
                    info = Encoding.UTF8.GetBytes("ENCRYPTS" + i);
                }

                byte[] key = DeriveHKDFKey(baseKey, KEY_SIZE_32, salt, info);
                keys.Add(key);
            }

            return keys;
        }

        public static void GenerateKeys(ref List<byte[]> encrypts, ref List<byte[]> signs, ref byte[] srcOut, byte[] secret, byte[] salt, int n)
        {
            byte[] src = DeriveHKDF8Key(secret, salt, Encoding.UTF8.GetBytes("SRC"));

            salt = src;

            byte[] sign = DeriveHKDF32Key(secret, salt, Encoding.UTF8.GetBytes("SIGN"));

            byte[] encrypt = DeriveHKDF32Key(secret, salt, Encoding.UTF8.GetBytes("ENCRYPT"));

            encrypts = GenerateNKeys(n, salt, KeyType.ENCRYPT, encrypt);
            signs = GenerateNKeys(n, salt, KeyType.SIGN, sign);

            srcOut = new byte[src.Length];
            Array.Copy(src, srcOut, src.Length);
        }

        private static byte[] ComputeHash(byte[] key, byte[] data)
        {
            var hmac = new HMACSHA256(key);

            return hmac.ComputeHash(data);
        }

        public static bool HashIsValid(byte[] key, byte[] data, byte[] hmacResult)
        {
            ReadOnlySpan<byte> hashBytes = ComputeHash(key, data);

            return CryptographicOperations.FixedTimeEquals(hashBytes, hmacResult);
        }

        public static string ConvertStringToBase64(string encoded)
        {
            encoded = encoded.Replace('-', '+').Replace('_', '/');
            var d = encoded.Length % 4;
            if (d != 0)
            {
                encoded = encoded.TrimEnd('=');
                encoded += d % 2 > 0 ? "=" : "==";
            }

            return encoded;
        }

        public static byte[] CBORBinaryStringToBytes(string s)
        {
            return Convert.FromBase64String(ConvertStringToBase64(s));
        }


        public static ECDHKeyPair CreateECDH()
        {
            var ecDiffieHellmanCng = new ECDiffieHellmanCng(CngKey.Create(CngAlgorithm.ECDiffieHellmanP256, null, new CngKeyCreationParameters { ExportPolicy = CngExportPolicies.AllowPlaintextExport }));
            var privateKey = ecDiffieHellmanCng.Key.Export(CngKeyBlobFormat.EccPrivateBlob);
            var publickey = ecDiffieHellmanCng.Key.Export(CngKeyBlobFormat.EccPublicBlob);
            ECDHKeyPair keyPair = new ECDHKeyPair();

            keyPair.PublicKey = publickey;
            keyPair.PrivateKey = privateKey;

            return keyPair;
        }

        public static byte[] Unwrap(byte[] wrappedData, byte[] key)
        {
            ICipherParameters keyParam = new KeyParameter(key);

            var symmetricBlockCipher = new AesEngine();
            Rfc3394WrapEngine wrapEngine = new Rfc3394WrapEngine(symmetricBlockCipher);

            wrapEngine.Init(false, keyParam);
            var unwrappedData = wrapEngine.Unwrap(wrappedData, 0, wrappedData.Length);

            return unwrappedData;
        }

        public static byte[] generateRawKey()
        {
            using (Aes aesAlgorithm = Aes.Create())
            {
                aesAlgorithm.KeySize = 256;
                aesAlgorithm.GenerateKey();
                string keyBase64 = Convert.ToBase64String(aesAlgorithm.Key);

                return aesAlgorithm.Key;
            }
        }

        static byte[] ComputeSha256Hash(byte[] rawData)
        {
            // Create a SHA256
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha256Hash.ComputeHash(rawData);

                return bytes;
            }
        }

        public static byte[] DeriveRawID(string code)
        {
            byte[] hashed = ComputeSha256Hash(Encoding.UTF8.GetBytes(code));
            byte[] salt = Encoding.UTF8.GetBytes("");
            byte[] info = Encoding.UTF8.GetBytes("ID");
            byte[] result = DeriveHKDF8Key(hashed, salt, info);

            return result;
        }

        static byte[] DeriveRawSecret(string code, byte[] ID)
        {
            byte[] hashed = ComputeSha256Hash(Encoding.UTF8.GetBytes(code));
            byte[] info = Encoding.UTF8.GetBytes("SECRET");
            byte[] result = DeriveHKDF32Key(hashed, ID, info);

            return result;
        }

        public static void GenerateOwnerKeys()
        {
            List<byte[]> encrypts = new List<byte[]>();
            List<byte[]> signs = new List<byte[]>();
            byte[] scr = new byte[SRC_SIZE_8];
            string ownerCode = OWNER_CODE;
            byte[] ownerID = DeriveRawID(ownerCode);


            string saltString = "";

            byte[] secret = DeriveRawSecret(ownerCode, ownerID);

            byte[] salt = Encoding.UTF8.GetBytes(saltString);

            GenerateKeys(ref encrypts, ref signs, ref scr, secret, salt, Servers.NUM_SERVERS);

            KeyStore.Inst.StoreENCRYPTS(ownerID, encrypts);
            KeyStore.Inst.StoreSIGNS(ownerID, signs);
        }
    }
}
