/*************************************************************************
*
* Copyright (c) 2015 Citrix Systems, Inc. All Rights Reserved.
* You may only reproduce, distribute, perform, display, or prepare derivative works of this file pursuant to a valid license from Citrix.
*
* THIS SAMPLE CODE IS PROVIDED BY CITRIX "AS IS" AND ANY EXPRESS OR IMPLIED
* WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
* MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
*
*************************************************************************/

using System;
using System.Configuration;
using System.Collections.Specialized;
using Citrix.DeliveryServices.ResourcesCommon.Customization.Contract;

namespace StoreCustomization_Input
{
  public class InputModifier : IInputModifier
  {
        public bool RunExtendedValidation { get { return false; } }
        public bool ReturnOriginalValueOnFailure { get { return false; } }

        public void Modify(out FarmSetsContext farmSetsContext,
                           out DeviceInfo deviceInfo,
                           out AccessConditions accessConditions,
                           CustomizationContextData context)
        {
            Tracer.TraceInfo("ClientNameRewrite: rewrite from rule customization");
            
            farmSetsContext = context.FarmSetsContext;
            deviceInfo = context.DeviceInfo;
            accessConditions = context.AccessConditions;

            // Read the client name rewrite rule from the app settings
            // eg
            //   <appSettings>
            //     <add key="clientNameRewriteRule" value="$U">
            //  Rules: 
            //      $U - user name
            //      $D - user domain
            //      $N - client name
            //      $R - Roaming status - "I":internal or "E":external
            //      $A - Detected Address
            //      $S - Supplied Address
            //      $V - DeviceID
            //      $G - gateway name (without domain)
            //      $P - platform (WIndows, MAc, LInux, IOs, ANdroid, CHromebook, BRowser, 
            //                     BLackberry, WindowsRt, WindowsPhone, UNknown)

            // Read template string from web.config <appSettings> element
            string clientNameRewriteRule = ConfigurationManager.AppSettings["clientNameRewriteRule"];
            if (clientNameRewriteRule != "")
            {
                Tracer.TraceInfo("ClientNameRewrite: using rule: " + clientNameRewriteRule);

                // check against illegal char list
                char[] BAD_CLIENTNAME_CHARS = "\"/\\[]:;|=,+*?<>".ToCharArray();
                if (clientNameRewriteRule.IndexOfAny(BAD_CLIENTNAME_CHARS) == -1)
                {
                    // valid template: fill it out from context
                    string newClientName = CNrewrite(clientNameRewriteRule, context);
                    Tracer.TraceInfo("ClientNameRewrite: client Name post rewrite = " + newClientName);
                    // overwrite device info
                    deviceInfo.ClientName = newClientName;
                }
                else
                    Tracer.TraceError("ClientNameRewrite: client name rule {0} contains illegal characters", clientNameRewriteRule);
            }
            else
                Tracer.TraceError("ClientNameRewrite: No Client Name rewrite rule supplied");


        }


      // fill out the client name by replacing tokens in the template with data from the context
    private static string CNrewrite(string clientNameRewriteRule, CustomizationContextData context)
    {
        string newClientName = string.Empty;
        char [] rule = clientNameRewriteRule.ToCharArray();
        for(int i = 0; i < rule.Length; i++)
        {
            if (rule[i] == '$')   // token match
            {
                switch (rule[++i])  // which token?
                {
                    case 'U':    // $U = UserName
                        {
                            // context username will be either just 'user' or may be UPN 'user@domain', in which case cut out just user name
                            int idx = context.UserIdentity.Name.IndexOf('@');
                            if (idx == -1)  // just user
                                newClientName += context.UserIdentity.Name;
                            else      // user@domain 
                                newClientName += context.UserIdentity.Name.Substring(0, idx);
                        }
                        break;

                    case 'V':    // $V = DeviceID
                        newClientName += context.DeviceInfo.DeviceId;
                        break;

                    case 'N':    // $N - current client name
                        // Note limitation here: if coming from a Browser, we don't have the machine name and because we require OverrideICAClientName switched on to get
                        // the result of this rewrite into the ICA file, that means the rewrite will have the generated WR_xxxxx client name.
                        newClientName += context.DeviceInfo.ClientName;
                        break;

                    case 'R':  //  $R - Roaming status - "I":internal or "E":external 
                        // infer from presence of gateway header
                        if (null == GetHeaderValueFromContext(context, "X-Citrix-Via"))
                             newClientName += 'I';
                        else
                             newClientName += 'E';
                        break;

                    case 'G':  //  $G - gateway name (without domain)
                        {
                            // X-Citrix-Via will have full fqdn of vserver (if connection external)
                            string gw = GetHeaderValueFromContext(context, "X-Citrix-Via");
                            if (null != gw)
                            {
                                int dot = gw.IndexOf('.');
                                if (dot != -1)
                                    newClientName += gw.Substring(0, dot);
                                else
                                    newClientName += gw;
                            }  // token rewrites to empty string no entry if no header found
                        }
                        break;

                    case 'A':    // $A - Detected Address
                        newClientName += context.DeviceInfo.DetectedAddress;
                        break;


                    case 'S':    // $S - Supplied Address
                        newClientName += context.DeviceInfo.SuppliedAddress;
                        break;

                    case 'P':    // $P - platform (WIndows, MAc, LInux, IOs, ANdroid, CHromebook, BRowser, 
                        //       BLackberry, WindowsRt, WindowsPhone, UNknown
                        {
                            string ua = GetHeaderValueFromContext(context, "User-Agent");
                            string platform = "UN";//UNknown;
                            if (ua != null)
                            {
                                if (ua.Contains("CitrixReceiver"))
                                {
                                    // Receiver user agent strings from  http://support.citrix.com/proddocs/topic/access-gateway-10/agee-clg-session-policies-overview-con.html
                                    if (ua.Contains("Windows"))
                                        platform = "WI";
                                    else if (ua.Contains("MacOSX"))
                                        platform = "MA";
                                    else if (ua.Contains("Linux"))
                                        platform = "LI";
                                    else if (ua.Contains("iOS"))
                                        platform = "IO";
                                    else if (ua.Contains("Android"))
                                        platform = "AN";
                                    else if (ua.Contains("Chromebook"))
                                        platform = "CH";
                                    else if (ua.Contains("Blackberry"))
                                        platform = "BL";
                                    else if (ua.Contains("WindowsPhone"))
                                        platform = "WP";
                                    else if (ua.Contains("WindowsRT"))
                                        platform = "WR";
                                }
                                else // browser case
                                    platform = "BR";


                            }
                            newClientName += platform;
                        }
                        break;

                    case 'D':  // user domain
                        {
                            // Can't get user domain from context user identity, instead use Current Claims Principal name (should be user@domain -or- domain\user)
                            string domain = "";
                            var identity = context.HttpContext.Items["Citrix.DeliveryServices.CurrentClaimsPrincipal"] as System.Security.Principal.IIdentity;
                            if (identity == null)
                            {
                                Tracer.TraceWarning("ClientNameRewrite: Can't get claims principal for domain (may be PNA path)");    /// may be using PNA 
                            }
                            else
                            {
                                Tracer.TraceInfo("ClientNameRewrite: claims principal Name:[{0}] Type:[{1}]", identity.Name, identity.AuthenticationType);
                                //string u = "", d = "";
                                int idx = identity.Name.IndexOf('\\');
                                if (idx != -1)  // domain\user
                                {
                                    domain = identity.Name.Substring(0, idx);
                                    //u = identity.Name.Substring(idx + 1);
                                }
                                else
                                {
                                    if ((idx = identity.Name.IndexOf('@')) != -1)  // user@domain
                                    {
                                        domain = identity.Name.Substring(idx + 1);
                                        // u = identity.Name.Substring(0, idx);
                                    }
                                    else
                                        Tracer.TraceError("ClientNameRewrite: ClientNameRewrite: Invalid form of user string: " + identity.Name);
                                }
                                newClientName += domain;
                            }
                        }
                        break;

                    default:
                        Tracer.TraceWarning("ClientNameRewrite: Client Name rewrite rule, unknown switch $" + rule[i]);
                        newClientName += "$" + rule[i];
                        break;

                }
            }
            else
                newClientName += rule[i];
                     
        }
        Tracer.TraceInfo("ClientNameRewrite: rewrite done: " + newClientName);
        // trim to 20 chars
        if (newClientName.Length > 20)   // keep this as seperate test in case need to handle as error condition 
        {
            newClientName = newClientName.Substring(0, 20);
            ///  Tracer.TraceInfo("ClientNameRewrite: post trim: " + newClientName);
        }

        return newClientName;
    }

    // utility fn: return the set of values associated with a given header, in a single string joined by ';'s
    // returns null if header is not found in context
    private static string GetHeaderValueFromContext(CustomizationContextData context, string headerName)
    {
        NameValueCollection reqHeads = context.HttpContext.Request.Headers;
        string targetH = headerName.ToLower();
        string values = "";
        bool found = false;
        foreach (string hr in reqHeads.AllKeys)
        {
            if (hr.ToLower() == targetH)
            {
                found = true;
                int h = 0;
                foreach (string hval in reqHeads.GetValues(hr))
                {
                    if (h++ > 1)
                    {
                       values += ";";
                    }
                    values += hval;
                }
            }
        }
        if (!found)
            return null;

        return values;
    }

  }


}


