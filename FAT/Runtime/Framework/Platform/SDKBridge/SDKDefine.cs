using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace FAT.Platform {
    [Flags]
    public enum ShareType
    {
        Session = 1,  // 聊天
        Timeline = 1<<1, // 朋友圈
        Link = 1<<8,
        Image = 1<<9,
    }
    
    public class IAPProductInfo
    {
        /// <summary>
        /// Id of the product.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Title of the product.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The product's detailed description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// ISO 4217 currency code for price.
        /// </summary>
        public string PriceCurrencyCode { get; set; }

        /// <summary>
        /// Formatted price of the item, including its currency sign.
        /// </summary>
        public string FormattedPrice { get; set; }

        /// <summary>
        /// Price in micro-units, where 1,000,000 micro-units equal one unit of the currency.
        /// </summary>
        public long Price { get; set; }
    }
}