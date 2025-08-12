# API Endpoint Changelog

Range: before merge 41c549b7 -> after merge 8f7ff006 (development)
Source: Merge PR #123 "update-entities" on Aug 9, 2025

Summary of changes impacting frontend: query params, HTTP methods, and request/response payloads.

- Links
  - GET /links
    - Query params
      - serviceProviderId (GUID, single) -> serviceProviderIds (list of GUIDs; pass multiple by repeating the query key).
      - technologyBandId (GUID, single) -> bandIds (list of GUIDs; pass multiple by repeating the query key).
    - Examples
      - Before: /links?pageNumber=1&pageSize=10&serviceProviderId=11111111-1111-1111-1111-111111111111&technologyBandId=22222222-2222-2222-2222-222222222222
      - After: /links?pageNumber=1&pageSize=10&serviceProviderIds=11111111-1111-1111-1111-111111111111&serviceProviderIds=33333333-3333-3333-3333-333333333333&bandIds=22222222-2222-2222-2222-222222222222&bandIds=44444444-4444-4444-4444-444444444444
  - POST /links
    - Body field rename: TechnologyBandId -> BandId.
  - Response shape
    - LinkDto.TechnologyBand -> LinkDto.Band.

- Networks
  - PATCH /networks/{id}
    - Method change: was PUT /networks/{id}, now PATCH /networks/{id}.
    - Body (UpdateNetworkDto): new optional fields
      - ULCenterFreq: decimal?
      - DLCenterFreq: decimal?
      - Bandwidth: decimal?
  - POST /networks
    - Body (CreateNetworkDto)
      - NetworkTemplateId: GUID? (was required GUID)
      - New required numeric fields: ULCenterFreq, DLCenterFreq, Bandwidth
  - Response shape
    - NetworkDto.NetworkTemplate is now nullable.
    - NetworkDto now includes ULCenterFreq, DLCenterFreq, Bandwidth.

- System Types
  - GET /system-types
    - Query params: added generationIds (list of GUIDs; pass multiple by repeating the query key) alongside technologyIds.
    - Example
      - After: /system-types?pageNumber=1&pageSize=10&technologyIds=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&technologyIds=bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb&generationIds=cccccccc-cccc-cccc-cccc-cccccccccccc&generationIds=dddddddd-dddd-dddd-dddd-dddddddddddd
  - POST /system-types
    - Body (CreateSystemTypeDto): added required GenerationId (GUID).

- Speedtests
  - POST /speedtests
    - Body (CreateSpeedtestDto)
      - GenerationId: GUID? (nullable)
      - New optional TechnologyId: GUID?
      - RSSI: decimal? (nullable)
      - SignalQuality: string? (was enum)
      - New IsFixedBroadband: bool
  - PATCH/PUT endpoints for speedtests not changed in controller; however UpdateSpeedtestDto changed:
    - SignalQuality: string? (was enum)
  - Response shape
    - SpeedtestDto now includes Technology and IsFixedBroadband.
    - SpeedtestDto.RSSI is now nullable.
    - SpeedtestDto.SignalQuality is now string (was enum).

Examples for frontend

Links

Create link

Before:

```json
{
  "type": "Microwave",
  "centerFrequency": 18.0,
  "lowerFrequency": 17.7,
  "upperFrequency": 18.3,
  "rxThreshold": 80.0,
  "technologyBandId": "22222222-2222-2222-2222-222222222222",
  "serviceProviderId": "11111111-1111-1111-1111-111111111111"
}
```

After:

```json
{
  "type": "Microwave",
  "centerFrequency": 18.0,
  "lowerFrequency": 17.7,
  "upperFrequency": 18.3,
  "rxThreshold": 80.0,
  "bandId": "22222222-2222-2222-2222-222222222222",
  "serviceProviderId": "11111111-1111-1111-1111-111111111111"
}
```

Fetch links (query)

Before:

```http
GET /links?pageNumber=1&pageSize=10&serviceProviderId=11111111-1111-1111-1111-111111111111&technologyBandId=22222222-2222-2222-2222-222222222222
```

After:

```http
GET /links?pageNumber=1&pageSize=10&serviceProviderIds=11111111-1111-1111-1111-111111111111&serviceProviderIds=33333333-3333-3333-3333-333333333333&bandIds=22222222-2222-2222-2222-222222222222&bandIds=44444444-4444-4444-4444-444444444444
```

Networks

Update network (method change and payload)

```http
PATCH /networks/{id}
```

Body:

```json
{
  "name": "Network A",
  "ulCenterFreq": 1930.0,
  "dlCenterFreq": 1845.0,
  "bandwidth": 20.0
}
```

Create network (new required fields, NetworkTemplateId optional)

```json
{
  "name": "Network A",
  "networkTemplateId": null,
  "systemTypeId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "serviceProviderId": "11111111-1111-1111-1111-111111111111",
  "uplinkId": "55555555-5555-5555-5555-555555555555",
  "downLinkId": "66666666-6666-6666-6666-666666666666",
  "ulCenterFreq": 1930.0,
  "dlCenterFreq": 1845.0,
  "bandwidth": 20.0
}
```

System Types

Fetch system types (query)

```http
GET /system-types?pageNumber=1&pageSize=10&technologyIds=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&technologyIds=bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb&generationIds=cccccccc-cccc-cccc-cccc-cccccccccccc&generationIds=dddddddd-dddd-dddd-dddd-dddddddddddd
```

Create system type (new required field)

```json
{
  "technologyId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "generationId": "cccccccc-cccc-cccc-cccc-cccccccccccc",
  "name": "LTE"
}
```

Speedtests

Create speedtest (payload changes)

```json
{
  "address": "123 Main St",
  "serviceProviderId": "11111111-1111-1111-1111-111111111111",
  "generationId": null,
  "technologyId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "dateTime": "2025-08-09T10:00:00Z",
  "ping": 30.5,
  "jitter": 5.1,
  "rssi": null,
  "uploadSpeed": 10.2,
  "downloadSpeed": 50.7,
  "latitude": 14.5995,
  "longitude": 120.9842,
  "mcc": "515",
  "mnc": "02",
  "tac": "12345",
  "signalQuality": "Good",
  "cellIdentifier": "Cell-ABC",
  "isFixedBroadband": true
}
```

Details by file diff

- NTC.QoS.API/Controllers/LinksController.cs: query param names switched to plural lists; request mapping updated.
- NTC.QoS.API/Controllers/NetworksController.cs: [HttpPut] -> [HttpPatch] for UpdateNetworkById.
- NTC.QoS.API/Controllers/SystemTypesController.cs: added generationIds query param and request mapping.
- DTOs updated in NTC.QoS.Domain and request classes in NTC.QoS.Application per items above.

New endpoints introduced

- None in this merge. Only modifications to existing endpoints in Links, Networks, and System Types.

Client migration guide

- Links
  - Update GET filters to use arrays: serviceProviderIds, bandIds. Use repeated query keys for multiple values.
  - Update Link create payload to use BandId. Expect response object to expose Band instead of TechnologyBand.
- Networks
  - Switch update calls to PATCH /networks/{id}.
  - Include ULCenterFreq, DLCenterFreq, Bandwidth in create payload; optionally in update payload.
  - NetworkTemplateId may be omitted in create payload.
- System Types
  - Optionally pass generationIds in list filters (use repeated query keys).
  - Include GenerationId when creating system types.
- Speedtests
  - When creating, optionally set TechnologyId; GenerationId is optional; RSSI may be omitted; SignalQuality should be a string; include IsFixedBroadband when applicable.
  - Expect additional fields in responses: technology, isFixedBroadband; and nullable rssi, string signalQuality.

Commit references

- Before: 41c549b7265ee5cb95bb87ab4c1e84f80f24bd5a
- After: 8f7ff0063b7d21e329f2df80cedb80f3f922b7e4
- PR: #123
