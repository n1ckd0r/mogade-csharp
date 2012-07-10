using System;
using System.Net;

#if IPHONE
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.SystemConfiguration;
using MonoTouch.CoreFoundation;
#elif WINDOWS_PHONE
using Microsoft.Phone.Net.NetworkInformation;
using System.Threading;
#else
using System.Net.NetworkInformation;
#endif

namespace Mogade {

public enum NetworkStatus {
 NotReachable,
 ReachableViaCarrierDataNetwork,
 ReachableViaWiFiNetwork
}

#if !IPHONE
public enum NetworkReachabilityFlags {
    Reachable,
}
#endif

public static class Reachability {
 public static string HostName = "www.google.com";

#if IPHONE
 public static bool IsReachableWithoutRequiringConnection (NetworkReachabilityFlags flags)
 {
     // Is it reachable with the current network configuration?
     bool isReachable = (flags & NetworkReachabilityFlags.Reachable) != 0;

     // Do we need a connection to reach it?
     bool noConnectionRequired = (flags & NetworkReachabilityFlags.ConnectionRequired) == 0;

     // Since the network stack will automatically try to get the WAN up,
     // probe that
     if ((flags & NetworkReachabilityFlags.IsWWAN) != 0)
         noConnectionRequired = true;

     return isReachable && noConnectionRequired;
 }

 // 
 // Raised every time there is an interesting reachable event, 
 // we do not even pass the info as to what changed, and 
 // we lump all three status we probe into one
 //
 public static event EventHandler ReachabilityChanged;

 static void OnChange (NetworkReachabilityFlags flags)
 {
     var h = ReachabilityChanged;
     if (h != null)
         h (null, EventArgs.Empty);
 }

 static NetworkReachability defaultRouteReachability;
 static bool IsNetworkAvaialable (out NetworkReachabilityFlags flags)
 {
     if (defaultRouteReachability == null){
         defaultRouteReachability = new NetworkReachability (new IPAddress (0));
         defaultRouteReachability.SetCallback (OnChange);
         defaultRouteReachability.Schedule (CFRunLoop.Current, CFRunLoop.ModeDefault);
     }
     if (defaultRouteReachability.TryGetFlags (out flags))
         return false;
     return IsReachableWithoutRequiringConnection (flags);
 }  
#endif

 // Is the host reachable with the current network configuration
 public static bool IsHostReachable (string host)
 {
     if (host == null || host.Length == 0)
         return false;

#if WINDOWS_PHONE
     //I know I know, but I'm trying to keep the behavior the same across platforms
     bool done = false;
     bool reachable = false;
     DeviceNetworkInformation.ResolveHostNameAsync(new DnsEndPoint(HostName, 80), (x) => {
         done = true;
         if (x.NetworkErrorCode == NetworkError.Success)
             reachable = true;
         else
             reachable = false;
     }, null);

     while (!done) { Thread.Sleep(200); }

     return reachable;    

#elif IPHONE
     
     using (var r = new NetworkReachability (host)){
         NetworkReachabilityFlags flags;

         if (r.TryGetFlags (out flags)){
             return IsReachableWithoutRequiringConnection (flags);
         }
     }
     return false;
#else
     var test = new Ping();
     var reply = test.Send(HostName);

     if (reply.Status == IPStatus.Success)
         return true;
     else
         return false;
#endif
 }

 public static NetworkStatus InternetConnectionStatus ()
 {
#if WINDOWS_PHONE
     if(DeviceNetworkInformation.IsCellularDataEnabled && DeviceNetworkInformation.IsNetworkAvailable)
         return NetworkStatus.ReachableViaCarrierDataNetwork;
     else if(DeviceNetworkInformation.IsWiFiEnabled && DeviceNetworkInformation.IsNetworkAvailable)
         return NetworkStatus.ReachableViaWiFiNetwork;
     else
         return NetworkStatus.NotReachable;
#elif IPHONE
     NetworkReachabilityFlags flags;

     bool defaultNetworkAvailable = IsNetworkAvaialable (out flags);
     if (defaultNetworkAvailable){
         if ((flags & NetworkReachabilityFlags.IsDirect) != 0)
             return NetworkStatus.NotReachable;
     } else if ((flags & NetworkReachabilityFlags.IsWWAN) != 0)
         return NetworkStatus.ReachableViaCarrierDataNetwork;
     return NetworkStatus.ReachableViaWiFiNetwork;
#else
     if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
         return NetworkStatus.ReachableViaWiFiNetwork;
     else
         return NetworkStatus.NotReachable;
#endif
 }

}
}

