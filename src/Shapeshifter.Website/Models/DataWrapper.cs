﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shapeshifter.Website.Models
{
    public class DataWrapper<T>
    {
		[JsonProperty("data")]
		public T Data { get; set; }
    }
}
