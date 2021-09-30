3.0.5 (2021-09-30)
------------------
 - Fixed: Kestrel throws an InvalidOperationException caused by sync Write()

3.0.4 (2021-09-23)
------------------
 - Fixed: bumped the System.Net.Http package to 4.3.4 to address the issue described here: https://github.com/dotnet/runtime/issues/28343

3.0.3 (2019-09-05)
------------------
 - Open sourced under 2-clause BSD license.

3.0.2 (2019-07-09)
------------------
 - New: defaults to 5 minute caching.
 - Fixed: CachingHttpHandler throwing exceptions when processing responses
   with no content.
 - Fixed: AspNetSample trying to use wrong System.Net.HttpClient.dll.

3.0.1 (2019-06-24)
------------------
 - Fixed: failing text due to Google DNS host name change.

3.0 (2019-06-24)
----------------
 - New: support for hostnames and globs. IWhitelist is introduced to provide
   this flexibility.

2.2 (2019-04-01)
----------------
 - Fixed: compatibility with Sitefinity by downgrading caching library.

2.1 (2019-01-14)
----------------
 - New: a short message including the IP is sent in the response on 403, to
   help troubleshooting.

2.0.1 (2019-01-08)
------------------
 - Fixed: cached responses are copied to protected the content against
   disposal.

2.0 (2019-01-08)
----------------
 - New: cache hosted whitelists if allowed by headers, as originally intended.
 - Fixed: sample page titles.
 - Breaking: net45 targets are now net461 for Microsoft.Extension.Caching.
 - Breaking: use of System.Net.Http package.

1.0 (2019-01-07)
----------------
Initial release.
