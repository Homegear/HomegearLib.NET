﻿using System;

namespace HomegearLib.RPC.Encoding
{
    public class RPCHeader
    {
        private string _authorization;
        public string Authorization
        {
            get { return _authorization; }
            set { _authorization = value; }
        }
    }
}
