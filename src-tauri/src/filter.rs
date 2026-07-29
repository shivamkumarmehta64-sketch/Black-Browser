pub const AD_DOMAINS: &[&str] = &[
    "doubleclick.net", "googlesyndication.com", "googleadservices.com",
    "pagead2.googlesyndication.com", "tpc.googlesyndication.com",
    "adservice.google.com", "2mdn.net", "google-analytics.com",
    "googletagmanager.com", "googletagservices.com", "adnxs.com",
    "rubiconproject.com", "criteo.com", "criteo.net", "pubmatic.com",
    "openx.net", "casalemedia.com", "moatads.com", "adsrvr.org",
    "sharethrough.com", "taboola.com", "outbrain.com", "amazon-adsystem.com",
    "sovrn.com", "indexww.com", "adsafeprotected.com", "contextweb.com",
    "lijit.com", "exelator.com", "bluekai.com", "agkn.com", "media.net",
    "revcontent.com", "zergnet.com", "advertising.com", "atdmt.com",
    "atwola.com", "bidswitch.net", "demdex.net", "krxd.net", "mathtag.com",
    "quantserve.com", "rlcdn.com", "ru4.com", "scorecardresearch.com",
    "serving-sys.com", "tynt.com", "adsymptotic.com", "appnexus.com",
    "adzerk.net", "exoclick.com", "propellerads.com", "popads.net",
    "onclickads.net", "mgid.com", "infolinks.com", "bidvertiser.com",
    "adf.ly", "adbrite.com", "adbutler.com", "adcash.com",
    "adlure.net", "admedia.com", "adperium.com", "adrecover.com",
    "adriot.com", "adroll.com", "adscale.de", "adserver.com",
    "adsmogo.com", "adspeed.net", "adspirit.de", "adtech.com",
    "adtech.de", "adventory.com", "adzerk.com", "affiliate.com",
    "bang.com", "bidclix.com", "buysellads.com", "carbonads.com",
    "clickbooth.com", "clicksor.com", "clicktrack.xyz", "codefund.com",
    "commissionjunction.com", "conversantmedia.com", "convertro.com",
    "cpalead.com", "cpxinteractive.com", "decknetwork.net",
    "dianomi.com", "dotandad.com", "dothomereports.com",
    "doublepimp.com", "dwin1.com", "epom.com", "etahub.com",
    "exponential.com", "eyenewton.ru", "fasternet.biz",
    "flashtalking.com", "fout.jp", "futureads.com",
    "g.doubleclick.net", "gemius.pl", "groovinads.com",
    "hooklogic.com", "hyperbanner.net", "i.liadm.com",
    "ibillboard.com", "igaworks.com", "improvedigital.com",
    "impulseclick.com", "inmobi.com", "intentiq.com",
    "interactivecircle.com", "iocnt.net", "ipromote.com",
    "justpremium.com", "kiosked.com", "leadboltads.net",
    "lockerdome.com", "madadsmedia.com", "mbistream.com",
    "trafficfactory.net", "trafficjunky.com", "trafficstars.com",
    "tynt.com", "undertone.com", "valueclickmedia.com",
    "veeseo.com", "vibrantmedia.com", "videoegg.com",
    "vidible.tv", "visistat.com", "volume7.co", "voxel.wtf",
    "wp-monero.com", "xad.com", "yadro.ru", "yieldmo.com",
    "yieldtraffic.com", "zedo.com", "zmedia.com",
];

/// Check if a URL should be blocked
pub fn is_ad_url(url: &url::Url) -> bool {
    let host = url.host_str().unwrap_or("");
    let path = url.path();
    let full = format!("{}{}", host, path);
    AD_DOMAINS.iter().any(|d| full.contains(d))
}


