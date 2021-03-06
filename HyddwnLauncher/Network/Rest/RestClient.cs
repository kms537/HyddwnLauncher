﻿using System;
using System.Collections.Generic;

namespace HyddwnLauncher.Network.Rest
{
    internal class RestClient
    {
        private int _maxRetryCount;
        public bool RequiresBase64Encode { get; protected set; }

        public RestClient(Uri baseUrl, string accessToken, bool requiresBase64Encode = false)
        {
            BaseUrl = baseUrl;
            AccessToken = accessToken;
            DefaultQueryString = new List<KeyValuePair<string, string>>();
            RequiresBase64Encode = requiresBase64Encode;

            MaxRetryCount = 0;
        }

        internal Uri BaseUrl { get; }
        internal List<KeyValuePair<string, string>> DefaultQueryString { get; }
        internal string AccessToken { get; set; }

        /// <exception cref="ArgumentOutOfRangeException" accessor="set">Condition.</exception>
        public int MaxRetryCount
        {
            get => _maxRetryCount;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _maxRetryCount = value;
            }
        }

        public void AddDefaultQueryString(string key, string value)
        {
            DefaultQueryString.Add(new KeyValuePair<string, string>(key, value));
        }

        public RestRequest Create(string endpoint)
        {
            return new RestRequest(this, endpoint, AccessToken);
        }
    }
}