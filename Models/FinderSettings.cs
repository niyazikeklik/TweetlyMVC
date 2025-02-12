﻿namespace Tweetly_MVC.Models
{
    public class FinderSettings
    {
        public bool CheckUseDB { get; set; }
        public bool CheckClearDB { get; set; }
        public bool CheckDetayGetir { get; set; }
        public bool CheckErkek { get; set; }
        public bool CheckKadin { get; set; }
        public bool CheckUnisex { get; set; }
        public bool CheckBelirsiz { get; set; }
        public bool CheckTakipEtmediklerim { get; set; }
        public bool CheckTakipEttiklerim { get; set; }
        public bool CheckBeniTakipEtmeyenler { get; set; }
        public bool CheckGizliHesap { get; set; }
        public bool CheckBeniTakipEdenler { get; set; }
        public bool CheckUseAllDriver { get; set; }
        public int TextTweetControl { get; set; }
        public int TextBulunacakKisiSayisi { get; set; }
    }
}