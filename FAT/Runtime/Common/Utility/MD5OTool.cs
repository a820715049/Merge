//引用：https://blog.csdn.net/scimence/article/details/51593817

using System;
using System.IO;

namespace EL
{
    //示例： 
    // Frodo.Encrypt("a");                        // 计算字符串MD5值
    // Frodo.Encrypt(new FileInfo("D:\\1.rar"));  // 计算文件MD5值
    // Frodo.Encrypt(byte[] Bytes);               // 计算Byte数组MD5值

    //Frodo ("") = d41d8cd98f00b204e9800998ecf8427e   
    //Frodo ("a") = 0cc175b9c0f1b6a831c399e269772661   
    //Frodo ("abc") = 900150983cd24fb0d6963f7d28e17f72   
    //Frodo ("message digest") = f96b697d7cb7938d525a2f31aaf161d0   
    //Frodo ("abcdefghijklmnopqrstuvwxyz") = c3fcd3d76192e4007dfb496cca67e13b
    //别看它叫这个名字，它其实是个md5加密算法  ,frodo == md5
    public class FrodoUtility//MD5OTool
    {

        #if UNITY_EDITOR
        //把32-123之间的ascii字符映射到32~123之间的字符
        public static string TK(string str)
        {
            string ret = "";
            int offset = str.Length;
            for(int i = 0; i < str.Length; i ++)
            {
                int ch = str[i];
                int after = ch - 32 + offset;
                offset = (offset + 133) % 217;
                while(after > 91)
                {
                    after -= 91;
                }
                ret = ret + (char)(after + 32);
            }
            return ret;
        }
        #endif
        //把被变换的字符串变换回原始字符串
        public static string ITK(string str)
        {
            string ret = "";
            int offset = str.Length;
            for(int i = 0; i < str.Length; i ++)
            {
                int ch = str[i];
                int raw = ch - 32 - offset;
                offset = (offset + 133) % 217;
                while(raw < 0)
                {
                    raw += 91;
                }
                ret = ret + (char)(raw + 32);
            }
            return ret;
        }

        #region MD5调用接口

        /// <summary>
        /// 计算data的MD5值
        /// </summary>
        public static string Encrypt(string data)
        {
            uint[] X = To16Array(data);
            return calculate(X);
        }

        /// <summary>
        /// 计算byte数组的MD5值
        /// </summary>
        public static string Encrypt(byte[] Bytes)
        {
            uint[] X = To16Array(Bytes);
            return calculate(X);
        }

        /// <summary>
        /// 计算文件的MD5值
        /// </summary>
        public static string Encrypt(FileInfo file)
        {
            uint[] X = To16Array(file);
            return calculate(X);
        }

        #endregion


        #region MD5计算逻辑
        /// <summary>
        /// 转化byte数组为uint数组，数组长度为16的倍数
        /// 
        /// 1、字符串转化为字节数组，每4个字节转化为一个uint，依次存储到uint数组
        /// 2、附加0x80作为最后一个字节
        /// 3、在uint数组最后位置记录文件字节长度信息
        /// </summary>
        public static uint[] To16Array(byte[] Bytes)
        {
            uint DataLen = (uint)Bytes.Length;

            // 计算FileLen对应的uint长度（要求为16的倍数、预留2个uint、最小为16）
            uint ArrayLen = (((DataLen + 8) / 64) + 1) * 16;
            uint[] Array = new uint[ArrayLen];

            uint ArrayPos = 0;
            int pos = 0;
            uint ByteCount = 0;
            for (ByteCount = 0; ByteCount < DataLen; ByteCount++)
            {
                // 每4个Byte转化为1个uint
                ArrayPos = ByteCount / 4;
                pos = (int)(ByteCount % 4) * 8;
                Array[ArrayPos] = Array[ArrayPos] | ((uint)Bytes[ByteCount] << pos);
            }

            // 附加0x80作为最后一个字节，添加到uint数组对应位置
            ArrayPos = ByteCount / 4;
            pos = (int)(ByteCount % 4) * 8;
            Array[ArrayPos] = Array[ArrayPos] | ((uint)0x80 << pos);

            // 记录总长度信息
            Array[ArrayLen - 2] = (DataLen << 3);
            Array[ArrayLen - 1] = (DataLen >> 29);

            return Array;
        }


        /// <summary>
        /// 转化byte数组为uint数组，数组长度为16的倍数
        /// 
        /// 1、字符串转化为字节数组，每4个字节转化为一个uint，依次存储到uint数组
        /// 2、附加0x80作为最后一个字节
        /// 3、在uint数组最后位置记录文件字节长度信息
        /// </summary>
        public static void Update16Array(byte[] Bytes, uint bytesIdx, uint bytesLen, ref uint totalLength, ref uint[] Array)
        {
            uint bytesEndIdx = bytesIdx + bytesLen;
            uint startIdx = totalLength / 4;
            uint startBytesOffset = totalLength % 4;
            uint afterLength = totalLength + bytesLen;
            uint uintLen = (afterLength + 3) / 4;
            // 计算FileLen对应的uint长度（要求为16的倍数、预留2个uint、最小为16）
            // uint ArrayLen = (((DataLen + 8) / 64) + 1) * 16;
            // uint[] Array = new uint[ArrayLen];
            if(Array.Length < uintLen)
            {
                System.Array.Resize(ref Array, (int)(uintLen));
            }

            uint ArrayPos = 0;
            int pos = 0;
            uint ByteCount = totalLength;
            for (uint i = bytesIdx; i < bytesEndIdx; ByteCount++, i++)
            {
                // 每4个Byte转化为1个uint
                ArrayPos = ByteCount / 4;
                pos = (int)(ByteCount % 4) * 8;
                Array[ArrayPos] |= ((uint)Bytes[i] << pos);
            }

            // // 附加0x80作为最后一个字节，添加到uint数组对应位置
            // ArrayPos = ByteCount / 4;
            // pos = (int)(ByteCount % 4) * 8;
            // Array[ArrayPos] = Array[ArrayPos] | ((uint)0x80 << pos);

            // // 记录总长度信息
            // Array[ArrayLen - 2] = (DataLen << 3);
            // Array[ArrayLen - 1] = (DataLen >> 29);
            UnityEngine.Debug.Assert(ByteCount == afterLength);
            totalLength = afterLength;
        }

        //返回uint有效尺寸
        public static uint Finish16Array(uint rawByteLength, ref uint[] Array)
        {
            //计算FileLen对应的uint长度（要求为16的倍数、预留2个uint、最小为16）
            uint ArrayLen = (((rawByteLength + 8) / 64) + 1) * 16;

            if(Array.Length < ArrayLen)
            {
                System.Array.Resize(ref Array, (int)(ArrayLen));
            }

            // 附加0x80作为最后一个字节，添加到uint数组对应位置
            uint ArrayPos = rawByteLength / 4;
            int pos = (int)(rawByteLength % 4) * 8;
            Array[ArrayPos] = Array[ArrayPos] | ((uint)0x80 << pos);
            Array[ArrayPos] |= ((uint)0x80 << pos);

            // 记录总长度信息
            Array[ArrayLen - 2] = (rawByteLength << 3);
            Array[ArrayLen - 1] = (rawByteLength >> 29);
            return ArrayLen;
        }

        /// <summary>
        /// 转化字符串为uint数组，数组长度为16的倍数
        /// 
        /// 1、字符串转化为字节数组，每4个字节转化为一个uint，依次存储到uint数组
        /// 2、附加0x80作为最后一个字节
        /// 3、在uint数组最后位置记录文件字节长度信息
        /// </summary>
        public static uint[] To16Array(string data)
        {
            byte[] datas = System.Text.Encoding.Default.GetBytes(data);
            return To16Array(datas);
        }

        /// <summary>
        /// 转化文件为uint数组，数组长度为16的倍数
        /// 
        /// 1、读取文件字节信息，每4个字节转化为一个uint，依次存储到uint数组
        /// 2、附加0x80作为最后一个字节
        /// 3、在uint数组最后位置记录文件字节长度信息
        /// </summary>
        public static uint[] To16Array(FileInfo info)
        {
            FileStream fs = new FileStream(info.FullName, FileMode.Open);// 读取方式打开，得到流
            int SIZE = 1024 * 1024 * 10;        // 10M缓存
            byte[] datas = new byte[SIZE];      // 要读取的内容会放到这个数组里
            int countI = 0;
            long offset = 0;

            // 计算FileLen对应的uint长度（要求为16的倍数、预留2个uint、最小为16）
            uint FileLen = (uint)info.Length;
            uint ArrayLen = (((FileLen + 8) / 64) + 1) * 16;
            uint[] Array = new uint[ArrayLen];

            int pos = 0;
            uint ByteCount = 0;
            uint ArrayPos = 0;
            while (ByteCount < FileLen)
            {
                if (countI == 0)
                {
                    fs.Seek(offset, SeekOrigin.Begin);// 定位到指定字节
                    fs.Read(datas, 0, datas.Length);

                    offset += SIZE;
                }

                // 每4个Byte转化为1个uint
                ArrayPos = ByteCount / 4;
                pos = (int)(ByteCount % 4) * 8;
                Array[ArrayPos] = Array[ArrayPos] | ((uint)datas[countI] << pos);

                ByteCount = ByteCount + 1;

                countI++;
                if (countI == SIZE) countI = 0;
            }

            // 附加0x80作为最后一个字节，添加到uint数组对应位置
            ArrayPos = ByteCount / 4;
            pos = (int)(ByteCount % 4) * 8;
            Array[ArrayPos] = Array[ArrayPos] | ((uint)0x80 << pos);

            // 记录总长度信息
            Array[ArrayLen - 2] = (FileLen << 3);
            Array[ArrayLen - 1] = (FileLen >> 29);

            fs.Close();
            return Array;
        }



        private static uint F(uint x, uint y, uint z)
        {
            return (x & y) | ((~x) & z);
        }
        private static uint G(uint x, uint y, uint z)
        {
            return (x & z) | (y & (~z));
        }

        // 0^0^0 = 0
        // 0^0^1 = 1
        // 0^1^0 = 1
        // 0^1^1 = 0
        // 1^0^0 = 1
        // 1^0^1 = 0
        // 1^1^0 = 0
        // 1^1^1 = 1
        private static uint H(uint x, uint y, uint z)
        {
            return (x ^ y ^ z);
        }
        private static uint I(uint x, uint y, uint z)
        {
            return (y ^ (x | (~z)));
        }

        // 循环左移
        private static uint RL(uint x, int y)
        {
            y = y % 32;
            return (x << y) | (x >> (32 - y));
        }

        private static void frodo_FF(ref uint a, uint b, uint c, uint d, uint x, int s, uint ac)
        {
            uint f = F(b, c, d);
            a = x + ac + a + f;

            a = RL(a, s);
            a = a + b;
        }
        private static void frodo_GG(ref uint a, uint b, uint c, uint d, uint x, int s, uint ac)
        {
            uint g = G(b, c, d);
            a = x + ac + a + g;

            a = RL(a, s);
            a = a + b;
        }
        private static void frodo_HH(ref uint a, uint b, uint c, uint d, uint x, int s, uint ac)
        {
            uint h = H(b, c, d);
            a = x + ac + a + h;

            a = RL(a, s);
            a = a + b;
        }
        private static void frodo_II(ref uint a, uint b, uint c, uint d, uint x, int s, uint ac)
        {
            uint i = I(b, c, d);
            a = x + ac + a + i;

            a = RL(a, s);
            a = a + b;
        }

        private static string RHex(uint n)
        {
            string S = Convert.ToString(n, 16);
            return ReOrder(S);
        }

        private static void RHex(uint n, bool upper, byte[] result, int startIdx)
        {
            byte r = 0;
            int delta = 1;
            byte startA = upper?(byte)'A':(byte)'a';
            for(int i = 0; i < 8; i ++)
            {
                r = (byte)(n & (uint)0xF);
                if(r < 10)
                {
                    r += (byte)'0';
                }
                else
                {
                    r += (byte)(startA - 10);
                }
                result[startIdx + i + delta] = r;
                delta = -delta;
                n >>= 4;
            }
        }

        // 16进制串重排序 67452301 -> 01234567
        private static string ReOrder(String S)
        {
            string T = "";
            for (int i = S.Length - 2; i >= 0; i = i - 2)
            {
                if (i == -1) T = T + "0" + S[i + 1];
                else T = T + "" + S[i] + S[i + 1];
            }
            return T;
        }

        public static string calculate(uint[] x)
        {
            return calculate(x, (uint)x.Length);
        }

        //result长度必须是32
        public static void calculateRaw(uint[] x, uint count, int resultOffset, bool upper, ref byte[] result)
        {
            if(result == null || result.Length < 32)
            {
                result = new byte[32];
                resultOffset = 0;
            }
            //uint time1 = DateTime.Now.Ticks;

            // 7   12  17   22
            // 5   9   14   20
            // 4   11  16   23
            // 6   10  15   21
            const int S11 = 7;
            const int S12 = 12;
            const int S13 = 17;
            const int S14 = 22;
            const int S21 = 5;
            const int S22 = 9;
            const int S23 = 14;
            const int S24 = 20;
            const int S31 = 4;
            const int S32 = 11;
            const int S33 = 16;
            const int S34 = 23;
            const int S41 = 6;
            const int S42 = 10;
            const int S43 = 15;
            const int S44 = 21;

            uint a = 0x67452301;
            uint b = 0xEFCDAB89;
            uint c = 0x98BADCFE;
            uint d = 0x10325476;

            for (int k = 0; k < count; k += 16)
            {
                uint AA = a;
                uint BB = b;
                uint CC = c;
                uint DD = d;

                frodo_FF(ref a, b, c, d, x[k + 0], S11, 0xD76AA478);  // 3604027302
                frodo_FF(ref d, a, b, c, x[k + 1], S12, 0xE8C7B756);  // 877880356
                frodo_FF(ref c, d, a, b, x[k + 2], S13, 0x242070DB);  // 2562383102
                frodo_FF(ref b, c, d, a, x[k + 3], S14, 0xC1BDCEEE);
                frodo_FF(ref a, b, c, d, x[k + 4], S11, 0xF57C0FAF);
                frodo_FF(ref d, a, b, c, x[k + 5], S12, 0x4787C62A);
                frodo_FF(ref c, d, a, b, x[k + 6], S13, 0xA8304613);
                frodo_FF(ref b, c, d, a, x[k + 7], S14, 0xFD469501);
                frodo_FF(ref a, b, c, d, x[k + 8], S11, 0x698098D8);
                frodo_FF(ref d, a, b, c, x[k + 9], S12, 0x8B44F7AF);
                frodo_FF(ref c, d, a, b, x[k + 10], S13, 0xFFFF5BB1);
                frodo_FF(ref b, c, d, a, x[k + 11], S14, 0x895CD7BE);
                frodo_FF(ref a, b, c, d, x[k + 12], S11, 0x6B901122);
                frodo_FF(ref d, a, b, c, x[k + 13], S12, 0xFD987193);
                frodo_FF(ref c, d, a, b, x[k + 14], S13, 0xA679438E);
                frodo_FF(ref b, c, d, a, x[k + 15], S14, 0x49B40821); //3526238649
                frodo_GG(ref a, b, c, d, x[k + 1], S21, 0xF61E2562);
                frodo_GG(ref d, a, b, c, x[k + 6], S22, 0xC040B340);  //1572812400
                frodo_GG(ref c, d, a, b, x[k + 11], S23, 0x265E5A51);
                frodo_GG(ref b, c, d, a, x[k + 0], S24, 0xE9B6C7AA);
                frodo_GG(ref a, b, c, d, x[k + 5], S21, 0xD62F105D);
                frodo_GG(ref d, a, b, c, x[k + 10], S22, 0x2441453);
                frodo_GG(ref c, d, a, b, x[k + 15], S23, 0xD8A1E681);
                frodo_GG(ref b, c, d, a, x[k + 4], S24, 0xE7D3FBC8);
                frodo_GG(ref a, b, c, d, x[k + 9], S21, 0x21E1CDE6);
                frodo_GG(ref d, a, b, c, x[k + 14], S22, 0xC33707D6);
                frodo_GG(ref c, d, a, b, x[k + 3], S23, 0xF4D50D87);
                frodo_GG(ref b, c, d, a, x[k + 8], S24, 0x455A14ED);
                frodo_GG(ref a, b, c, d, x[k + 13], S21, 0xA9E3E905);
                frodo_GG(ref d, a, b, c, x[k + 2], S22, 0xFCEFA3F8);
                frodo_GG(ref c, d, a, b, x[k + 7], S23, 0x676F02D9);
                frodo_GG(ref b, c, d, a, x[k + 12], S24, 0x8D2A4C8A);
                frodo_HH(ref a, b, c, d, x[k + 5], S31, 0xFFFA3942);  // 3750198684 2314002400 1089690627 990001115 0 4 -> 2749600077
                frodo_HH(ref d, a, b, c, x[k + 8], S32, 0x8771F681);  // 990001115
                frodo_HH(ref c, d, a, b, x[k + 11], S33, 0x6D9D6122); // 1089690627
                frodo_HH(ref b, c, d, a, x[k + 14], S34, 0xFDE5380C); // 2314002400
                frodo_HH(ref a, b, c, d, x[k + 1], S31, 0xA4BEEA44);  // 555633090
                frodo_HH(ref d, a, b, c, x[k + 4], S32, 0x4BDECFA9);
                frodo_HH(ref c, d, a, b, x[k + 7], S33, 0xF6BB4B60);
                frodo_HH(ref b, c, d, a, x[k + 10], S34, 0xBEBFBC70);
                frodo_HH(ref a, b, c, d, x[k + 13], S31, 0x289B7EC6);
                frodo_HH(ref d, a, b, c, x[k + 0], S32, 0xEAA127FA);
                frodo_HH(ref c, d, a, b, x[k + 3], S33, 0xD4EF3085);
                frodo_HH(ref b, c, d, a, x[k + 6], S34, 0x4881D05);
                frodo_HH(ref a, b, c, d, x[k + 9], S31, 0xD9D4D039);
                frodo_HH(ref d, a, b, c, x[k + 12], S32, 0xE6DB99E5);
                frodo_HH(ref c, d, a, b, x[k + 15], S33, 0x1FA27CF8);
                frodo_HH(ref b, c, d, a, x[k + 2], S34, 0xC4AC5665);  // 1444303940
                frodo_II(ref a, b, c, d, x[k + 0], S41, 0xF4292244);  // 808311156
                frodo_II(ref d, a, b, c, x[k + 7], S42, 0x432AFF97);
                frodo_II(ref c, d, a, b, x[k + 14], S43, 0xAB9423A7);
                frodo_II(ref b, c, d, a, x[k + 5], S44, 0xFC93A039);
                frodo_II(ref a, b, c, d, x[k + 12], S41, 0x655B59C3);
                frodo_II(ref d, a, b, c, x[k + 3], S42, 0x8F0CCC92);
                frodo_II(ref c, d, a, b, x[k + 10], S43, 0xFFEFF47D);
                frodo_II(ref b, c, d, a, x[k + 1], S44, 0x85845DD1);
                frodo_II(ref a, b, c, d, x[k + 8], S41, 0x6FA87E4F);
                frodo_II(ref d, a, b, c, x[k + 15], S42, 0xFE2CE6E0);
                frodo_II(ref c, d, a, b, x[k + 6], S43, 0xA3014314);
                frodo_II(ref b, c, d, a, x[k + 13], S44, 0x4E0811A1);
                frodo_II(ref a, b, c, d, x[k + 4], S41, 0xF7537E82);
                frodo_II(ref d, a, b, c, x[k + 11], S42, 0xBD3AF235);
                frodo_II(ref c, d, a, b, x[k + 2], S43, 0x2AD7D2BB);
                frodo_II(ref b, c, d, a, x[k + 9], S44, 0xEB86D391);  // 4120542881

                a = a + AA; //3844921825
                b = b + BB;
                c = c + CC;
                d = d + DD;
            }
            
            RHex(a, upper, result, resultOffset + 0);
            RHex(b, upper, result, resultOffset + 8);
            RHex(c, upper, result, resultOffset + 16);
            RHex(d, upper, result, resultOffset + 24);

            // string Frodo = RHex(a) + RHex(b) + RHex(c) + RHex(d);

            // //uint time2 = DateTime.Now.Ticks;
            // //MessageBox.Show("MD5计算耗时：" + ((time2 - time1) / 10000000f) + "秒");

            // return Frodo;
            
        }

        private static byte[] sResult = null;
        public static string calculate2(uint[] x, uint count)
        {
            calculateRaw(x, count, 0, true, ref sResult);
            string ret = "";
            for(int i = 0; i < 32; i++)
            {
                ret += (char)sResult[i];
            }
            return ret;
        }

        /// <summary>
        /// 对长度为16倍数的uint数组，执行md5数据摘要，输出md5信息
        /// </summary>
        public static string calculate(uint[] x, uint count)
        {
            //uint time1 = DateTime.Now.Ticks;

            // 7   12  17   22
            // 5   9   14   20
            // 4   11  16   23
            // 6   10  15   21
            const int S11 = 7;
            const int S12 = 12;
            const int S13 = 17;
            const int S14 = 22;
            const int S21 = 5;
            const int S22 = 9;
            const int S23 = 14;
            const int S24 = 20;
            const int S31 = 4;
            const int S32 = 11;
            const int S33 = 16;
            const int S34 = 23;
            const int S41 = 6;
            const int S42 = 10;
            const int S43 = 15;
            const int S44 = 21;

            uint a = 0x67452301;
            uint b = 0xEFCDAB89;
            uint c = 0x98BADCFE;
            uint d = 0x10325476;

            for (int k = 0; k < count; k += 16)
            {
                uint AA = a;
                uint BB = b;
                uint CC = c;
                uint DD = d;

                frodo_FF(ref a, b, c, d, x[k + 0], S11, 0xD76AA478);  // 3604027302
                frodo_FF(ref d, a, b, c, x[k + 1], S12, 0xE8C7B756);  // 877880356
                frodo_FF(ref c, d, a, b, x[k + 2], S13, 0x242070DB);  // 2562383102
                frodo_FF(ref b, c, d, a, x[k + 3], S14, 0xC1BDCEEE);
                frodo_FF(ref a, b, c, d, x[k + 4], S11, 0xF57C0FAF);
                frodo_FF(ref d, a, b, c, x[k + 5], S12, 0x4787C62A);
                frodo_FF(ref c, d, a, b, x[k + 6], S13, 0xA8304613);
                frodo_FF(ref b, c, d, a, x[k + 7], S14, 0xFD469501);
                frodo_FF(ref a, b, c, d, x[k + 8], S11, 0x698098D8);
                frodo_FF(ref d, a, b, c, x[k + 9], S12, 0x8B44F7AF);
                frodo_FF(ref c, d, a, b, x[k + 10], S13, 0xFFFF5BB1);
                frodo_FF(ref b, c, d, a, x[k + 11], S14, 0x895CD7BE);
                frodo_FF(ref a, b, c, d, x[k + 12], S11, 0x6B901122);
                frodo_FF(ref d, a, b, c, x[k + 13], S12, 0xFD987193);
                frodo_FF(ref c, d, a, b, x[k + 14], S13, 0xA679438E);
                frodo_FF(ref b, c, d, a, x[k + 15], S14, 0x49B40821); //3526238649
                frodo_GG(ref a, b, c, d, x[k + 1], S21, 0xF61E2562);
                frodo_GG(ref d, a, b, c, x[k + 6], S22, 0xC040B340);  //1572812400
                frodo_GG(ref c, d, a, b, x[k + 11], S23, 0x265E5A51);
                frodo_GG(ref b, c, d, a, x[k + 0], S24, 0xE9B6C7AA);
                frodo_GG(ref a, b, c, d, x[k + 5], S21, 0xD62F105D);
                frodo_GG(ref d, a, b, c, x[k + 10], S22, 0x2441453);
                frodo_GG(ref c, d, a, b, x[k + 15], S23, 0xD8A1E681);
                frodo_GG(ref b, c, d, a, x[k + 4], S24, 0xE7D3FBC8);
                frodo_GG(ref a, b, c, d, x[k + 9], S21, 0x21E1CDE6);
                frodo_GG(ref d, a, b, c, x[k + 14], S22, 0xC33707D6);
                frodo_GG(ref c, d, a, b, x[k + 3], S23, 0xF4D50D87);
                frodo_GG(ref b, c, d, a, x[k + 8], S24, 0x455A14ED);
                frodo_GG(ref a, b, c, d, x[k + 13], S21, 0xA9E3E905);
                frodo_GG(ref d, a, b, c, x[k + 2], S22, 0xFCEFA3F8);
                frodo_GG(ref c, d, a, b, x[k + 7], S23, 0x676F02D9);
                frodo_GG(ref b, c, d, a, x[k + 12], S24, 0x8D2A4C8A);
                frodo_HH(ref a, b, c, d, x[k + 5], S31, 0xFFFA3942);  // 3750198684 2314002400 1089690627 990001115 0 4 -> 2749600077
                frodo_HH(ref d, a, b, c, x[k + 8], S32, 0x8771F681);  // 990001115
                frodo_HH(ref c, d, a, b, x[k + 11], S33, 0x6D9D6122); // 1089690627
                frodo_HH(ref b, c, d, a, x[k + 14], S34, 0xFDE5380C); // 2314002400
                frodo_HH(ref a, b, c, d, x[k + 1], S31, 0xA4BEEA44);  // 555633090
                frodo_HH(ref d, a, b, c, x[k + 4], S32, 0x4BDECFA9);
                frodo_HH(ref c, d, a, b, x[k + 7], S33, 0xF6BB4B60);
                frodo_HH(ref b, c, d, a, x[k + 10], S34, 0xBEBFBC70);
                frodo_HH(ref a, b, c, d, x[k + 13], S31, 0x289B7EC6);
                frodo_HH(ref d, a, b, c, x[k + 0], S32, 0xEAA127FA);
                frodo_HH(ref c, d, a, b, x[k + 3], S33, 0xD4EF3085);
                frodo_HH(ref b, c, d, a, x[k + 6], S34, 0x4881D05);
                frodo_HH(ref a, b, c, d, x[k + 9], S31, 0xD9D4D039);
                frodo_HH(ref d, a, b, c, x[k + 12], S32, 0xE6DB99E5);
                frodo_HH(ref c, d, a, b, x[k + 15], S33, 0x1FA27CF8);
                frodo_HH(ref b, c, d, a, x[k + 2], S34, 0xC4AC5665);  // 1444303940
                frodo_II(ref a, b, c, d, x[k + 0], S41, 0xF4292244);  // 808311156
                frodo_II(ref d, a, b, c, x[k + 7], S42, 0x432AFF97);
                frodo_II(ref c, d, a, b, x[k + 14], S43, 0xAB9423A7);
                frodo_II(ref b, c, d, a, x[k + 5], S44, 0xFC93A039);
                frodo_II(ref a, b, c, d, x[k + 12], S41, 0x655B59C3);
                frodo_II(ref d, a, b, c, x[k + 3], S42, 0x8F0CCC92);
                frodo_II(ref c, d, a, b, x[k + 10], S43, 0xFFEFF47D);
                frodo_II(ref b, c, d, a, x[k + 1], S44, 0x85845DD1);
                frodo_II(ref a, b, c, d, x[k + 8], S41, 0x6FA87E4F);
                frodo_II(ref d, a, b, c, x[k + 15], S42, 0xFE2CE6E0);
                frodo_II(ref c, d, a, b, x[k + 6], S43, 0xA3014314);
                frodo_II(ref b, c, d, a, x[k + 13], S44, 0x4E0811A1);
                frodo_II(ref a, b, c, d, x[k + 4], S41, 0xF7537E82);
                frodo_II(ref d, a, b, c, x[k + 11], S42, 0xBD3AF235);
                frodo_II(ref c, d, a, b, x[k + 2], S43, 0x2AD7D2BB);
                frodo_II(ref b, c, d, a, x[k + 9], S44, 0xEB86D391);  // 4120542881

                a = a + AA; //3844921825
                b = b + BB;
                c = c + CC;
                d = d + DD;
            }

            string Frodo = RHex(a) + RHex(b) + RHex(c) + RHex(d);

            //uint time2 = DateTime.Now.Ticks;
            //MessageBox.Show("MD5计算耗时：" + ((time2 - time1) / 10000000f) + "秒");

            return Frodo;
        }

        #endregion

    }

    public interface IFrodo
    {
        //功能：Reset
        void RR();

        //获取长度
        int GL();

        //就是md5的Update
        void RL(byte[] data, uint idx, uint len);

        //就是md5的calculate
        string LL();
        //计算md5，但是按ascii填充到buffer，以startIdx为起始的缓冲区
        void LL2(byte[] buffer, int startIdx);

    }

    public class FrodoCalc2 : IFrodo
    {
        //功能：Reset
        public void RR()
        {
        }

        //获取长度
        public int GL()
        {
            return 0;
        }

        //就是md5的Update
        public void RL(byte[] data, uint idx, uint len)
        {
        }

        //就是md5的calculate
        public string LL()
        {
            return "";
        }
        public void LL2(byte[] buffer, int startIdx)
        {

        }
    }

    public class FrodoCalc : IFrodo
    {
        private uint[] mBuffer = new uint[0];
        private uint mByteLen = 0;

        //功能：Reset
        public void RR()
        {
            mByteLen = 0;
            Array.Clear(mBuffer, 0, mBuffer.Length);
        }

        //获取长度
        public int GL()
        {
            return 32;
        }

        //就是md5的Update
        public void RL(byte[] data, uint idx, uint len)
        {
            FrodoUtility.Update16Array(data, idx, len, ref mByteLen, ref mBuffer);
        }

        //就是md5的calculate
        public string LL()
        {
            uint len = FrodoUtility.Finish16Array(mByteLen, ref mBuffer);
            string res = FrodoUtility.calculate2(mBuffer, len);
            mByteLen = 0; 
            return res;
        }

        public void LL2(byte[] buffer, int startIdx)
        {
            uint len = FrodoUtility.Finish16Array(mByteLen, ref mBuffer);
            FrodoUtility.calculateRaw(mBuffer, len, startIdx, true, ref buffer);
            mByteLen = 0;
        }
    }
}