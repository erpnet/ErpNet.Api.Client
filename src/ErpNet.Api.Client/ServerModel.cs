using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ErpNet.ServerModel
{
    /// <summary>
    /// A model class for {dbhost}/sys/auto-discovery endpoint.
    /// </summary>
    public class AutoDiscovery
    {
        /// <summary>
        /// A collection of web sites for the destination database.
        /// </summary>
        public AutoDiscoveryWebSite[]? WebSites { get; set; }
    }

    /// <summary>
    /// A model class for {dbhost}/sys/auto-discovery endpoint.
    /// </summary>
    [DebuggerDisplay("{Type} [{Status}] => {Url}")]
    public class AutoDiscoveryWebSite
    {
        /// <summary>
        /// The type of web site.
        /// </summary>
        public WebsiteType Type { get; set; }
        /// <summary>
        /// Working status.
        /// </summary>
        public WebsiteStatus Status { get; set; }
        /// <summary>
        /// Url of the web ite.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Additional properties specific for the web site.
        /// </summary>
        public Dictionary<string, string>? AdditionalProperties { get; set; }
    }

    /// <summary>
    /// Type of web site. This enum must be euivalent to WebSitesRepository.WebSiteType enum.
    /// </summary>
    public enum WebsiteType
    {
        /// <summary>API value. Stored as 'API'.</summary>
        API,
        /// <summary>ClientCenter value. Stored as 'CC'.</summary>
        ClientCenter,
        /// <summary>ID value. Stored as 'ID'. Open ID Provider. </summary>
        ID,
        /// <summary>ECommerce value. Stored as 'EC'.</summary>
        ECommerce,
        /// <summary>LEGALBG value. Stored as 'LEG'.</summary>
        LEGALBG,
        /// <summary>SocialInteractions value. Stored as 'SI'.</summary>
        SocialInteractions,
        /// <summary>DigitalMarketplace value. Stored as 'DM'.</summary>
        DigitalMarketplace,
        /// <summary>UserProfile value. Stored as 'PROFILE'.</summary>
        UserProfile,
        /// <summary>WebClientApplication value. Stored as 'APP'.</summary>
        WebClientApplication,
        /// <summary>Table API value. Stored as 'TAP'.</summary>
        TableAPI,
        /// <summary>Data Access API value. Stored as 'DAP'.</summary>
        DataAccessAPI,
        /// <summary>ERP.net WMS Mobile App. Stored as 'WMS'.</summary>
        WMSMobileApp
    }

    /// <summary>
    /// Information about the web site
    /// </summary>
    public enum WebsiteStatus
    {
        /// <summary>
        /// We haven't been able to determine the status yet
        /// </summary>
        Unknown,
        /// <summary>
        /// Process is working and we are able to ping the web site
        /// </summary>
        Working,
        /// <summary>
        /// Site is down, the process is unresponsive, or we can't ping the web site
        /// </summary>
        NotWorking
    }
}
