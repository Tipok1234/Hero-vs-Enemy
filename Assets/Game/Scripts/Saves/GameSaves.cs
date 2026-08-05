using UnityEngine;
using System;

namespace DataUtils
{
    public class GameSaves
    {
        public event Action ChangedCurrencyAction;
        
        private const string CoinKey = "Coin_Key";
        
        private static GameSaves _instance;

        public static GameSaves Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameSaves();
                }

                return _instance;
            }
        }

        public void AddCoin(int coin)
        {
            var coins = ReadData<int>(CoinKey);

            coins += coin;

            WriteData(CoinKey, coins);
            ChangedCurrencyAction?.Invoke();
        }
        
        public void RemoveCoin(int coin)
        {
            var coins = ReadData<int>(CoinKey);

            coins -= coin;
            
            WriteData(CoinKey,coins);
            ChangedCurrencyAction?.Invoke();
        }
        
        public int GetCoin() => ReadData<int>(CoinKey);

        public void WriteData<T>(string key, T data)
        {  
            if (typeof(T) == typeof(int))
            {
                PlayerPrefs.SetInt(key, (int)(object)data);
            }
            else if (typeof(T) == typeof(float))
            {
                PlayerPrefs.SetFloat(key, (float)(object)data);
            }
            else if (typeof(T) == typeof(string))
            {
                PlayerPrefs.SetString(key, (string)(object)data);
            }
            else if (typeof(T) == typeof(bool))
            {
                PlayerPrefs.SetInt(key, (bool)(object)data ? 1 : 0);
            }
            else
            {
                Debug.LogError($"Wrong data type: Attempted to write type {typeof(T).Name}");
            }

            PlayerPrefs.Save();
        }

        public T ReadData<T>(string key)
        {
            if (typeof(T) == typeof(int))
            {
                return (T)(object)PlayerPrefs.GetInt(key);
            }

            if (typeof(T) == typeof(float))
            {
                return (T)(object)PlayerPrefs.GetFloat(key);
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)PlayerPrefs.GetString(key);
            }

            if (typeof(T) == typeof(bool))
            {
                int intValue = PlayerPrefs.GetInt(key);
                return (T)(object)(intValue != 0);
            }

            Debug.LogError("There are no Saves");
            return default(T);
        }
    }
}
