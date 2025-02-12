﻿using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Tweetly_MVC.Cloud;
using Tweetly_MVC.Tweetly;
using Tweetly_MVC.Twitter;

namespace Tweetly_MVC.Init
{
    public static class CreateUser
    {
        public static bool Filter(this User profil)
        {
            if (profil.Cinsiyet == "Erkek" && !Repo.Ins.UserPrefs.CheckErkek)
                return false;
            if (profil.Cinsiyet == "Kadın" && !Repo.Ins.UserPrefs.CheckKadin)
                return false;
            if (profil.Cinsiyet == "Unisex" && !Repo.Ins.UserPrefs.CheckUnisex)
                return false;
            if (profil.Cinsiyet == "Belirsiz" && !Repo.Ins.UserPrefs.CheckBelirsiz)
                return false;

            if (profil.FollowStatus == "Takip et" && Repo.Ins.UserPrefs.CheckTakipEtmediklerim)
                return false;
            if (profil.FollowStatus == "Takip ediliyor" && Repo.Ins.UserPrefs.CheckTakipEttiklerim)
                return false;
            if (profil.FollowersStatus == "Takip etmiyor" && Repo.Ins.UserPrefs.CheckBeniTakipEtmeyenler)
                return false;
            if ((profil.FollowersStatus == "Seni takip ediyor" && Repo.Ins.UserPrefs.CheckBeniTakipEdenler) || (profil.IsPrivate && Repo.Ins.UserPrefs.CheckGizliHesap))
                return false;
            return true;
        }
        private static List<string> GetUrlsOfTweet(string username, int tweetCount)
        {

            List<string> TweetIds = new();
            Drivers.Driver.LinkeGit($"https://mobile.twitter.com/{username}");
            while (!Drivers.Driver.IsSayfaSonu() && TweetIds.Count < tweetCount)
            {
                object result = Drivers.Driver.JsRun("return document.querySelectorAll(\"a[id^='id__']\");");
                foreach (IWebElement item in (IReadOnlyCollection<IWebElement>)result)
                {
                    try
                    {
                        string url = item.GetAttribute("href");
                        if (url.Contains($"/{username}/status/") && !TweetIds.Contains(url))
                            TweetIds.Add(url);
                    }
                    catch (System.Exception) { continue; }

                }
            }
            return TweetIds;

        }
        public static List<User> BegenenleriGetir(string username)
        {
            List<User> Begenenler = new();
            List<string> TweetUrls = GetUrlsOfTweet(username, Repo.Ins.UserPrefs.TextTweetControl);
            foreach (string TweetUrl in TweetUrls)
                foreach (User profil in ListeGezici(link: TweetUrl + "/likes", detay: false))
                {
                    User x = Begenenler.FirstOrDefault(x => x.Username == profil.Username);
                    if (x != null) Begenenler.Remove(x);
                    else x = Drivers.MusaitOlanDriver().DetayGetir(profil);
                    x.BegeniSayisi++;
                    x.BegeniOrani = Math.Round((double)x.BegeniSayisi / TweetUrls.Count, 2);
                    x.Count = Begenenler.Count;
                    Begenenler.Add(x);
                }
            return Begenenler;

        }
        public static List<User> ListeGezici(string link, bool detay = true)
        {
            List<User> yerelList = new();
            IWebDriver driverr = Drivers.Driver.LinkeGit(link);
            List<string> kontrolEdildi = new();
            while (!driverr.IsSayfaSonu() && yerelList.Count < Repo.Ins.UserPrefs.TextBulunacakKisiSayisi)
                foreach (string element in Drivers.Driver.GetListelenenler())
                {
                    string html = element.Split(new string(Liste.sabit))[0];
                    string text = element.Split(new string(Liste.sabit))[1];
                    if (kontrolEdildi.Contains(text)) continue;
                    else kontrolEdildi.Add(text);
                    User profil = GetProfil(html, text);
                    if (profil is null) continue;
                    if (detay) profil = Drivers.MusaitOlanDriver().DetayGetir(profil);
                    profil.Count = yerelList.Count;
                    yerelList.Add(profil);
                    Repo.Ins.Iletisim.currentValue = yerelList.Count;
                }
            return yerelList;
        }
        public static User DetayGetir(this IWebDriver driver, User profil)
        {
            if (profil == null)
            {
                Drivers.kullanıyorum.Remove(driver);
                return null;
            }
            if (Repo.Ins.UserPrefs.CheckDetayGetir)
                return driver.GetProfil(profil);

            Drivers.kullanıyorum.Remove(driver);
            return profil;

        }
        public static User GetProfil(this IWebDriver driver, string username)
        {
            string link = $"https://mobile.twitter.com/{username}";
            driver.LinkeGit(link);
            User profil = new();
            if (Waiters.ProfilLoadControl(driver, link, 300000))
            {
                profil.Count = Repo.Ins.Liste.Count + 1;
                profil.Username = username;
                profil.TweetCount = driver.GetTweetCount();
                profil.Name = driver.GetName();
                profil.Date = driver.GetDate();
                profil.Location = driver.GetLocation();
                profil.PhotoURL = driver.GetPhotoURL(username);
                profil.Following = driver.GetFollowing();
                profil.Followers = driver.GetFollowers();
                profil.FollowersStatus = driver.IsFollowers();
                profil.FollowStatus = driver.GetfollowStatus();
                profil.Bio = driver.GetBio();
                profil.IsPrivate = driver.IsPrivate();
                profil.Cinsiyet = DetectGender.CinsiyetBul(profil.Name, profil.PhotoURL);
                profil.TweetSikligi = Profil.GetGunlukSiklik(profil.TweetCount, profil.Date);
                profil.LastTweetsDate = driver.GetSonEtkilesimOrtalama(profil.Date, profil.TweetCount);
                driver.JsRun("document.querySelector('[data-testid=ScrollSnap-List] > div:last-child a').click();");
                profil.LikeCount = driver.GetLikeCount();
                profil.BegeniSikligi = Profil.GetGunlukSiklik(profil.LikeCount, profil.Date);
                profil.LastLikesDate = driver.GetSonEtkilesimOrtalama(profil.Date, profil.LikeCount);
            }

            Drivers.kullanıyorum.Remove(driver);
            return profil;
        }
        private static User GetProfil(this IWebDriver driver, User profil)
        {

            User DBprofil = new DatabasesContext().Records.FirstOrDefault(x => x.Username == profil.Username);
            if (DBprofil != null && Repo.Ins.UserPrefs.CheckUseDB)
            {
                profil.TweetCount = DBprofil.TweetCount;
                profil.Date = DBprofil.Date;
                profil.Location = DBprofil.Location;
                profil.Following = DBprofil.Following;
                profil.Followers = DBprofil.Followers;
                profil.TweetSikligi = DBprofil.TweetSikligi;
                profil.LastTweetsDate = DBprofil.LastTweetsDate;
                profil.LikeCount = DBprofil.LikeCount;
                profil.BegeniSikligi = DBprofil.BegeniSikligi;
                profil.LastLikesDate = DBprofil.LastLikesDate;
                Drivers.kullanıyorum.Remove(driver);
                return profil;
            }


            string link = "https://mobile.twitter.com/" + profil.Username;
            driver.LinkeGit(link, false);

            if (Waiters.ProfilLoadControl(driver, link, 300000))
            {
                profil.TweetCount = driver.GetTweetCount();
                profil.Date = driver.GetDate();
                profil.Location = driver.GetLocation();
                profil.Following = driver.GetFollowing();
                profil.Followers = driver.GetFollowers();
                profil.TweetSikligi = Profil.GetGunlukSiklik(profil.TweetCount, profil.Date);
                profil.LastTweetsDate = driver.GetSonEtkilesimOrtalama(profil.Date, profil.TweetCount);

                driver.JsRun("document.querySelector('[data-testid=ScrollSnap-List] > div:last-child a').click();");

                profil.LikeCount = driver.GetLikeCount();
                profil.BegeniSikligi = Profil.GetGunlukSiklik(profil.LikeCount, profil.Date);
                profil.LastLikesDate = driver.GetSonEtkilesimOrtalama(profil.Date, profil.LikeCount);
            }
            Drivers.kullanıyorum.Remove(driver);
            return profil;
        }
        public static User GetProfil(string innerHTML, string innerText)
        {
            User profil = new();

            profil.Name = innerText.GetName();
            profil.PhotoURL = innerHTML.GetPhotoURL();
            profil.Cinsiyet = DetectGender.CinsiyetBul(profil.Name, profil.PhotoURL);
            profil.Username = innerText.GetUserName();
            profil.IsPrivate = innerHTML.İsPrivate();
            profil.FollowersStatus = innerText.GetFollowersStatus();
            profil.FollowStatus = innerText.GetFollowStatus();
            profil.Bio = innerHTML.GetBio();
            User dbprofil = new DatabasesContext().Records.FirstOrDefault(x => profil.Username == x.Username);
            profil.BegeniSayisi = dbprofil?.BegeniSayisi;
            profil.BegeniOrani = dbprofil?.BegeniOrani;

            profil = Filter(profil) ? profil : null;
            return profil;
        }
     
    }
}
