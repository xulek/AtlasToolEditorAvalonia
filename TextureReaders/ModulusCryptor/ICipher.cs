namespace AtlasToolEditorAvalonia.ModulusCryptor
{
    public interface ICipher
    {
        int GetBlockSize();
        void BlockDecrypt(byte[] inBuf, int len, byte[] outBuf);

        void Init();
    }

}
