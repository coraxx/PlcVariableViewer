using System;
using System.Collections.Generic;
using System.Diagnostics;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;

namespace TwinCatVariableViewer
{
    /// <summary>
    /// Methods for extracting/parsing symbols
    /// </summary>
    internal class Tc3Symbols
    {
        public static void AddSymbolRecursive(List<ISymbol> symbols, ISymbol symbol, bool debug = false)
        {
            try
            {
                if (symbol.DataType == null && symbol.Category != DataTypeCategory.Struct)
                    return;
                if (symbol.DataType != null && symbol.DataType.Name.StartsWith("TC2_MC2."))
                    return;
                if (symbol.TypeName != null && symbol.TypeName == "TC2_MC2.MC_Power")
                    Console.WriteLine();
                foreach (ITypeAttribute attribute in symbol.Attributes)
                {
                    if (debug) Debug.WriteLine($"{attribute.Name} : {attribute.Value}");
                }

                if (debug) Debug.WriteLine(
                    $"{symbol.InstancePath} : {symbol.TypeName} (IG: 0x{((IAdsSymbol)symbol).IndexGroup:x} IO: 0x{((IAdsSymbol)symbol).IndexOffset:x} size: {symbol.Size})");

                if (symbol.Category == DataTypeCategory.Array)
                {
                    IArrayInstance arrInstance = (IArrayInstance)symbol;
                    //IArrayType arrType = (IArrayType)symbol.DataType;

                    if (arrInstance.Elements != null)
                    {
                        // int count = 0;
                        foreach (ISymbol arrayElement in arrInstance.Elements)
                        {
                            AddSymbolRecursive(symbols, arrayElement);
                            
                            //count++;
                            //if (count > 20) // Write only the first 20 to limit output
                            //    break;
                        }
                    }
                    else Debug.WriteLine($"Array elements of {arrInstance.TypeName} are null");
                }
                else if (symbol.Category == DataTypeCategory.Struct)
                {
                    IStructInstance structInstance = (IStructInstance)symbol;
                    //IStructType structType = (IStructType)symbol.DataType;
                    try
                    {
                        foreach (ISymbol member in structInstance.MemberInstances)
                        {
                            AddSymbolRecursive(symbols, member);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
                else if (symbol.Category == DataTypeCategory.Reference)
                {
                    // "REFERENCE TO ..." cannot be read, so filter it out. Comes from InOut variables in function blocks
                    // pass
                }
                else
                {
                    symbols.Add(symbol);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public static string GetSymbolValue(ISymbol symbol, TcAdsClient plcClient)
        {
            if (plcClient == null || plcClient.ConnectionState != ConnectionState.Connected) return "No connection";
            string data = "";
            try
            {
                TimeSpan t;
                DateTime dt;
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
                    default:
                        if (symbol.TypeName.StartsWith("STRING"))
                        {
                            int charCount = Convert.ToInt32(symbol.TypeName.Replace("STRING(", "").Replace(")", ""));
                            data = plcClient.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(string), new[] { charCount }).ToString();
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

        public static string GetSymbolValue(ISymbol symbol, AdsConnection connection)
        {
            if (connection == null || connection.ConnectionState != ConnectionState.Connected) return "No connection";
            string data = "";
            try
            {
                TimeSpan t;
                DateTime dt;
                switch (symbol.TypeName)
                {
                    case "BOOL":
                        data = connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(bool)).ToString();
                        break;
                    case "BYTE":
                        data = connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(byte)).ToString();
                        break;
                    case "SINT":
                        data = connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(sbyte)).ToString();
                        break;
                    case "INT":
                        data = connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(short)).ToString();
                        break;
                    case "DINT":
                        data = connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(int)).ToString();
                        break;
                    case "LINT":
                        data = connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(long)).ToString();
                        break;
                    case "USINT":
                        data = connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(byte)).ToString();
                        break;
                    case "UINT":
                        data = connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(ushort)).ToString();
                        break;
                    case "UDINT":
                        data = connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)).ToString();
                        break;
                    case "ULINT":
                        data = connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(ulong)).ToString();
                        break;
                    case "REAL":
                        data = connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(float)).ToString();
                        break;
                    case "LREAL":
                        data = connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(double)).ToString();
                        break;
                    case "WORD":
                        data = connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(ushort)).ToString();
                        break;
                    case "DWORD":
                        data = connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)).ToString();
                        break;
                    case "TIME":
                        t = TimeSpan.FromMilliseconds((uint)connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)));
                        if (t.Minutes > 0) data = $"T#{t.Minutes}m{t.Seconds}s{t.Milliseconds}ms";
                        else if (t.Seconds > 0) data = $"T#{t.Seconds}s{t.Milliseconds}ms";
                        else data = $"T#{t.Milliseconds}ms";
                        break;
                    case "TIME_OF_DAY":
                    case "TOD":
                        t = TimeSpan.FromMilliseconds((uint)connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)));
                        if (t.Hours > 0) data = $"TOD#{t.Hours}:{t.Minutes}:{t.Seconds}.{t.Milliseconds}";
                        else if (t.Minutes > 0) data = $"TOD#{t.Minutes}:{t.Seconds}.{t.Milliseconds}";
                        else data = $"TOD#{t.Seconds}.{t.Milliseconds}";
                        break;
                    case "DATE":
                        dt = new DateTime(1970, 1, 1);
                        dt = dt.AddSeconds((uint)connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)));
                        data = $"D#{dt.Year}-{dt.Month}-{dt.Day}";
                        break;
                    case "DATE_AND_TIME":
                    case "DT":
                        dt = new DateTime(1970, 1, 1);
                        dt = dt.AddSeconds((uint)connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(uint)));
                        data = $"DT#{dt.Year}-{dt.Month}-{dt.Day}-{dt.Hour}:{dt.Minute}:{dt.Second}";
                        break;
                    default:
                        if (symbol.TypeName.StartsWith("STRING"))
                        {
                            int charCount = Convert.ToInt32(symbol.TypeName.Replace("STRING(", "").Replace(")", ""));
                            data = connection.ReadAny(((IAdsSymbol)symbol).IndexGroup, ((IAdsSymbol)symbol).IndexOffset, typeof(string), new[] { charCount }).ToString();
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

        public static string GetSymbolValue(SymbolInfo symbol, TcAdsClient plcClient)
        {
            if (plcClient == null || plcClient.ConnectionState != ConnectionState.Connected) return "No connection";
            string data = "";
            try
            {
                TimeSpan t;
                DateTime dt;
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

        public static string GetSymbolValue(SymbolInfo symbol, AdsConnection connection)
        {
            if (connection == null || connection.ConnectionState != ConnectionState.Connected) return "No connection";
            string data;
            try
            {
                TimeSpan t;
                DateTime dt;
                switch (symbol.Type)
                {
                    case "BOOL":
                        data = connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(bool)).ToString();
                        break;
                    case "BYTE":
                        data = connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(byte)).ToString();
                        break;
                    case "SINT":
                        data = connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(sbyte)).ToString();
                        break;
                    case "INT":
                        data = connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(short)).ToString();
                        break;
                    case "DINT":
                        data = connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(int)).ToString();
                        break;
                    case "LINT":
                        data = connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(long)).ToString();
                        break;
                    case "USINT":
                        data = connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(byte)).ToString();
                        break;
                    case "UINT":
                        data = connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(ushort)).ToString();
                        break;
                    case "UDINT":
                        data = connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(uint)).ToString();
                        break;
                    case "ULINT":
                        data = connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(ulong)).ToString();
                        break;
                    case "REAL":
                        data = connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(float)).ToString();
                        break;
                    case "LREAL":
                        data = connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(double)).ToString();
                        break;
                    case "WORD":
                        data = connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(ushort)).ToString();
                        break;
                    case "DWORD":
                        data = connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(uint)).ToString();
                        break;
                    case "ENUM":
                        data = connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(IEnumValue)).ToString();
                        break;
                    case "TIME":
                        t = TimeSpan.FromMilliseconds((uint)connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(uint)));
                        if (t.Minutes > 0) data = $"T#{t.Minutes}m{t.Seconds}s{t.Milliseconds}ms";
                        else if (t.Seconds > 0) data = $"T#{t.Seconds}s{t.Milliseconds}ms";
                        else data = $"T#{t.Milliseconds}ms";
                        break;
                    case "TIME_OF_DAY":
                    case "TOD":
                        t = TimeSpan.FromMilliseconds((uint)connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(uint)));
                        if (t.Hours > 0) data = $"TOD#{t.Hours}:{t.Minutes}:{t.Seconds}.{t.Milliseconds}";
                        else if (t.Minutes > 0) data = $"TOD#{t.Minutes}:{t.Seconds}.{t.Milliseconds}";
                        else data = $"TOD#{t.Seconds}.{t.Milliseconds}";
                        break;
                    case "DATE":
                        dt = new DateTime(1970, 1, 1);
                        dt = dt.AddSeconds((uint)connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(uint)));
                        data = $"D#{dt.Year}-{dt.Month}-{dt.Day}";
                        break;
                    case "DATE_AND_TIME":
                    case "DT":
                        dt = new DateTime(1970, 1, 1);
                        dt = dt.AddSeconds((uint)connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(uint)));
                        data = $"DT#{dt.Year}-{dt.Month}-{dt.Day}-{dt.Hour}:{dt.Minute}:{dt.Second}";
                        break;
                    default:
                        if (symbol.Type.StartsWith("STRING"))
                        {
                            int charCount = Convert.ToInt32(symbol.Type.Replace("STRING(", "").Replace(")", ""));
                            data = connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(string), new[] { charCount }).ToString();
                        }
                        else
                        {
                            data = connection.ReadAny((uint)symbol.IndexGroup, (uint)symbol.IndexOffset, typeof(string), new[] { symbol.Size }).ToString();
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
