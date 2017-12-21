Rewriting the Client Name using a template.

Note: there is a restriction here for users using a browser to access StoreFront. The ClientName that is used as input for this case will be the StoreFront generated one (WR_xxxxx).The reason for this is that the browser cannot provide the client machine name to the StoreFront service. Without the customisation in place, the client name that winds up in the session is controlled by the overrideICAClientName setting – if set, the client name will be the SF generated one (it’s passed back to the client in the ICA launch params). If this setting is false, SF doesn’t pass anything to the client and the client will pass the computer name as part of native ICA negotiation. For the customisation to work, the override setting must be on (so that the new client name is passed back to the client). Native Receivers supply their client name to StoreFront as part of the launch params and do not suffer the same restriction.

To use this feature, you’ll need to copy the customisation DLL into the appropriate Store directory and provide the relevant template via config. 

The customisation template can be any string, but where that string contains a particular token, the token will be replaced by some information from the User Context, as  shown by the table below:

Token: Corresponding Data
$U     User name
$D     User domain
$N     Existing Client Name variable
$R     Roaming status – "I":internal or "E":external
$A     Detected Address
$S     Supplied Address
$V     DeviceID
$G     Gateway name (without domain). Empty string if the connection is not made through a gateway.
$P     Receiver Platform (WIndows, MAc, LInux, IOs, ANdroid, CHromebook, BRowser,
BLackberry, WindowsRt, WindowsPhone, UNknown)

$<anything else> is copied into the result.

A couple of examples:

If the intent was just to replace the ClientName with the user name, the template is then just "$U".

If you wanted to use the existing ClientName but know whether the connection was through a gateway then you could use "$N_$R" which would result in the ClientName with a '_I' or '_E' suffix.

Normal Client Name restrictions apply: max 20 chars (anything over will be truncated), and no reserved characters from the following list: "/ []:;|=\,+*?<>. Any template containing these characters will be rejected.

To install the DLL

Locate the existing placeholder DLL for the customisation SDK in your deployment (\inetpub\wwwroot\Citrix\storename\bin\StoreCustomization_Input.dll). Save this file so that you can restore your deployment to default if required. Move the backup file to another directory, do not rename it within the same directory to avoid risks of DLL conflicts. Copy the StoreCustomization_Input.dll from the download zip into the bin directory. Note: if you are using a StoreFront group, perform the propagation action to push the new assembly to other members of the group.

To create the template

Locate the existing web.config fie for the Store (\inetpub\wwwroot\Citrix\storename\web.config).

Take a backup.

Edit the file in a text editor.

Locate the following line in the file

<appSettings>

Change this XML to look like the following

<appSettings>
<add key="clientNameRewriteRule" value="$N" />
</appSettings>

Remember to make sure the first line no longer includes a trailing slash.

Now locate the <launch> element in the file that looks like the following (you won’t see the ..., sections, those removed for simplicity)

 <launch setNoLoadBiasFlag=”off” addressResolutionType=”DNS-port” ... overrideIcaClientName=”on” requireLaunchReference=”on” ... >

Change the overrideIcaClientName setting to “on”.

Save the file changes.

If you are using a Storefront group, now propagate these changes to all members of the group.

With the above example setting. the deployment should now operate exactly the same as before! What you now can do is alter the template value from the “$N” above as you wish and note the changes in the supplied client name. Changes will take place on the next operation after the web.config file has been saved.


