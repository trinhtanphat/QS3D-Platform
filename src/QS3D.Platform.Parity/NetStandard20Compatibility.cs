namespace QS3D.Platform.Parity;

internal static class NetStandard20DictionaryExtensions
{
    internal static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value) where TKey : notnull
    {
        if (dictionary is null) throw new ArgumentNullException(nameof(dictionary));
        if (dictionary.ContainsKey(key)) return false;
        dictionary.Add(key, value);
        return true;
    }
}
