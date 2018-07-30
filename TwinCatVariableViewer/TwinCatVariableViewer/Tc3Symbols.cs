using System;
using System.Collections.Generic;
using System.Diagnostics;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;

namespace TwinCatVariableViewer
{
    internal class Tc3Symbols
    {
        public static void AddSymbolRecursive(List<ISymbol> symbols, ISymbol symbol, bool debug = false)
        {
            // IDataType type = symbol.DataType as IDataType;

            foreach (ITypeAttribute attribute in symbol.Attributes)
            {
                if (debug) Debug.WriteLine($"{attribute.Name} : {attribute.Value}");
            }

            if (debug) Debug.WriteLine(
                $"{symbol.InstancePath} : {symbol.TypeName} (IG: 0x{((IAdsSymbol) symbol).IndexGroup:x} IO: 0x{((IAdsSymbol) symbol).IndexOffset:x} size: {symbol.Size})");

            if (symbol.Category == DataTypeCategory.Array)
            {
                IArrayInstance arrInstance = (IArrayInstance)symbol;
                // IArrayType arrType = (IArrayType)symbol.DataType;

                int count = 0;

                foreach (ISymbol arrayElement in arrInstance.Elements)
                {
                    AddSymbolRecursive(symbols, arrayElement);
                    count++;

                    if (count > 20) // Write only the first 20 to limit output
                        break;
                }
            }
            else if (symbol.Category == DataTypeCategory.Struct)
            {
                IStructInstance structInstance = (IStructInstance)symbol;
                // IStructType structType = (IStructType)symbol.DataType;

                foreach (ISymbol member in structInstance.MemberInstances)
                {
                    AddSymbolRecursive(symbols, member);
                }
            }
            else
            {
                // "REFERENCE TO ..." cannot be read, so filter it out. Comes from InOut variables in function blocks
                if (!symbol.TypeName.Contains("REFERENCE")) symbols.Add(symbol);
            }
        }

        public static string GetSymbolValue(ISymbol symbol, TcAdsClient plcClient)
        {
            if (plcClient == null || plcClient.ConnectionState != ConnectionState.Connected) return "No connection";
            string data = "";
            TimeSpan t;
            DateTime dt;
            try
            {
                switch (symbol.TypeName)
                {
                    case "BOOL":
                        data = plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(bool)).ToString();
                        break;
                    case "BYTE":
                        data = plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(byte)).ToString();
                        break;
                    case "SINT":
                        data = plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(sbyte)).ToString();
                        break;
                    case "INT":
                        data = plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(short)).ToString();
                        break;
                    case "DINT":
                        data = plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(int)).ToString();
                        break;
                    case "LINT":
                        data = plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(long)).ToString();
                        break;
                    case "USINT":
                        data = plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(byte)).ToString();
                        break;
                    case "UINT":
                        data = plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(ushort)).ToString();
                        break;
                    case "UDINT":
                        data = plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)).ToString();
                        break;
                    case "ULINT":
                        data = plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(ulong)).ToString();
                        break;
                    case "REAL":
                        data = plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(float)).ToString();
                        break;
                    case "LREAL":
                        data = plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(double)).ToString();
                        break;
                    case "WORD":
                        data = plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(ushort)).ToString();
                        break;
                    case "DWORD":
                        data = plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)).ToString();
                        break;
                    case "TIME":
                        t = TimeSpan.FromMilliseconds((uint)plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)));
                        if (t.Minutes > 0) data = $"T#{t.Minutes}m{t.Seconds}s{t.Milliseconds}ms";
                        else if (t.Seconds > 0) data = $"T#{t.Seconds}s{t.Milliseconds}ms";
                        else data = $"T#{t.Milliseconds}ms";
                        break;
                    case "TIME_OF_DAY":
                    case "TOD":
                        t = TimeSpan.FromMilliseconds((uint)plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)));
                        if (t.Hours > 0) data = $"TOD#{t.Hours}:{t.Minutes}:{t.Seconds}.{t.Milliseconds}";
                        else if (t.Minutes > 0) data = $"TOD#{t.Minutes}:{t.Seconds}.{t.Milliseconds}";
                        else data = $"TOD#{t.Seconds}.{t.Milliseconds}";
                        break;
                    case "DATE":
                        dt = new DateTime(1970, 1, 1);
                        dt = dt.AddSeconds((uint)plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)));
                        data = $"D#{dt.Year}-{dt.Month}-{dt.Day}";
                        break;
                    case "DATE_AND_TIME":
                    case "DT":
                        dt = new DateTime(1970, 1, 1);
                        dt = dt.AddSeconds((uint)plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)));
                        data = $"DT#{dt.Year}-{dt.Month}-{dt.Day}-{dt.Hour}:{dt.Minute}:{dt.Second}";
                        break;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return data;
        }

        public static string GetSymbolValue(SymbolInfo symbol, TcAdsClient plcClient)
        {
            if (plcClient == null || plcClient.ConnectionState != ConnectionState.Connected) return "No connection";
            string data = "";
            TimeSpan t;
            DateTime dt;
            try
            {
                switch (symbol.Type)
                {
                    case "BOOL":
                        data = plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(bool)).ToString();
                        break;
                    case "BYTE":
                        data = plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(byte)).ToString();
                        break;
                    case "SINT":
                        data = plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(sbyte)).ToString();
                        break;
                    case "INT":
                        data = plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(short)).ToString();
                        break;
                    case "DINT":
                        data = plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(int)).ToString();
                        break;
                    case "LINT":
                        data = plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(long)).ToString();
                        break;
                    case "USINT":
                        data = plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(byte)).ToString();
                        break;
                    case "UINT":
                        data = plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(ushort)).ToString();
                        break;
                    case "UDINT":
                        data = plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(uint)).ToString();
                        break;
                    case "ULINT":
                        data = plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(ulong)).ToString();
                        break;
                    case "REAL":
                        data = plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(float)).ToString();
                        break;
                    case "LREAL":
                        data = plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(double)).ToString();
                        break;
                    case "WORD":
                        data = plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(ushort)).ToString();
                        break;
                    case "DWORD":
                        data = plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(uint)).ToString();
                        break;
                    case "TIME":
                        t = TimeSpan.FromMilliseconds((uint)plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(uint)));
                        if (t.Minutes > 0) data = $"T#{t.Minutes}m{t.Seconds}s{t.Milliseconds}ms";
                        else if (t.Seconds > 0) data = $"T#{t.Seconds}s{t.Milliseconds}ms";
                        else data = $"T#{t.Milliseconds}ms";
                        break;
                    case "TIME_OF_DAY":
                    case "TOD":
                        t = TimeSpan.FromMilliseconds((uint)plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(uint)));
                        if (t.Hours > 0) data = $"TOD#{t.Hours}:{t.Minutes}:{t.Seconds}.{t.Milliseconds}";
                        else if (t.Minutes > 0) data = $"TOD#{t.Minutes}:{t.Seconds}.{t.Milliseconds}";
                        else data = $"TOD#{t.Seconds}.{t.Milliseconds}";
                        break;
                    case "DATE":
                        dt = new DateTime(1970, 1, 1);
                        dt = dt.AddSeconds((uint)plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(uint)));
                        data = $"D#{dt.Year}-{dt.Month}-{dt.Day}";
                        break;
                    case "DATE_AND_TIME":
                    case "DT":
                        dt = new DateTime(1970, 1, 1);
                        dt = dt.AddSeconds((uint)plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(uint)));
                        data = $"DT#{dt.Year}-{dt.Month}-{dt.Day}-{dt.Hour}:{dt.Minute}:{dt.Second}";
                        break;
                    default:
                        if (symbol.Type.StartsWith("STRING"))
                        {
                            int charCount = Convert.ToInt32(symbol.Type.Replace("STRING(", "").Replace(")", ""));
                            data = plcClient.ReadAny(symbol.IndexGroup, symbol.IndexOffset, typeof(string), new[] { charCount }).ToString();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return data;
        }
    }
}
