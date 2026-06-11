# Signal Level Entries API

Base URL pattern: `/signal-levels/{signalLevelId}/entries`

All endpoints require a valid JWT bearer token and the respective permission.

---

## Data Shapes

### SignalLevelEntry (response)

| Field          | Type     | Nullable | Description                                 |
|----------------|----------|----------|---------------------------------------------|
| `id`           | `uuid`   | No       | Unique identifier of the entry              |
| `signalLevelId`| `uuid`   | No       | Parent signal level                         |
| `color`        | `string` | No       | Hex color code (e.g. `#1A7A1A`)             |
| `thresholdFrom`| `number` | Yes      | Lower bound of the signal range (e.g. -100) |
| `thresholdTo`  | `number` | Yes      | Upper bound of the signal range (e.g. -89)  |
| `description`  | `string` | No       | Label for this level (e.g. `"Fair"`)        |
| `sortOrder`    | `number` | No       | Display order (ascending)                   |

### CreateSignalLevelEntryDto (request body)

| Field          | Type     | Required | Description                          |
|----------------|----------|----------|--------------------------------------|
| `color`        | `string` | ✅       | Hex color string                     |
| `thresholdFrom`| `number` | ❌       | Lower dBm bound                      |
| `thresholdTo`  | `number` | ❌       | Upper dBm bound                      |
| `description`  | `string` | ✅       | Human-readable label                 |
| `sortOrder`    | `number` | ✅       | Integer sort position                |

### UpdateSignalLevelEntryDto (request body)

All fields are optional — only include what you want to change.

| Field          | Type     | Description         |
|----------------|----------|---------------------|
| `color`        | `string` | Hex color string    |
| `thresholdFrom`| `number` | Lower dBm bound     |
| `thresholdTo`  | `number` | Upper dBm bound     |
| `description`  | `string` | Human-readable label|
| `sortOrder`    | `number` | Sort position       |

---

## Endpoints

### POST `/signal-levels/{signalLevelId}/entries`
**Permission:** `signal_level_entries.create`

Creates a new signal level entry under the given signal level.

**Request**
```json
POST /signal-levels/5eb782e3-8edf-46fa-b0ea-f3ce0aa79b96/entries
Content-Type: application/json

{
  "color": "#FF0000",
  "thresholdFrom": -110,
  "thresholdTo": -99,
  "description": "Weak",
  "sortOrder": 4
}
```

**Success response `200 OK`**
```json
{
  "succeeded": true,
  "message": "Signal level entry created successfully.",
  "data": {
    "id": "b9e0cf8b-c40a-4ce7-8dd9-9b937e62c90e",
    "signalLevelId": "5eb782e3-8edf-46fa-b0ea-f3ce0aa79b96",
    "color": "#FF0000",
    "thresholdFrom": -110,
    "thresholdTo": -99,
    "description": "Weak",
    "sortOrder": 4
  },
  "errors": {}
}
```

**Duplicate error `400 Bad Request`**
```json
{
  "succeeded": false,
  "message": "Duplicate signal level entry is not allowed.",
  "data": null,
  "errors": {
    "SignalLevelEntry": [
      "An identical signal level entry already exists for this signal level."
    ]
  }
}
```

**Parent not found `400 Bad Request`**
```json
{
  "succeeded": false,
  "message": "Signal level not found.",
  "data": null,
  "errors": {
    "SignalLevelId": [
      "Signal level with the specified Id does not exist."
    ]
  }
}
```

---

### GET `/signal-levels/{signalLevelId}/entries`
**Permission:** `signal_level_entries.view`

Returns a paginated list of entries for the signal level.

**Query params**

| Param        | Type   | Default | Description        |
|--------------|--------|---------|--------------------|
| `pageSize`   | `int`  | `10`    | Items per page     |
| `pageNumber` | `int`  | `1`     | 1-based page index |

**Success response `200 OK`**
```json
{
  "pageSize": 10,
  "pageNumber": 1,
  "totalPageNumber": 1,
  "loadMore": false,
  "totalRows": 5,
  "succeeded": true,
  "message": "Signal Level Entry records successfully retrieved.",
  "data": [
    {
      "id": "955aa4ac-b40c-4f18-84c6-a982d7c8b992",
      "signalLevelId": "5eb782e3-8edf-46fa-b0ea-f3ce0aa79b96",
      "color": "#1A7A1A",
      "thresholdFrom": -80,
      "thresholdTo": 0,
      "description": "Excellent",
      "sortOrder": 1
    }
  ],
  "errors": {}
}
```

---

### GET `/signal-levels/{signalLevelId}/entries/{id}`
**Permission:** `signal_level_entries.view`

Returns a single entry by ID.

**Success response `200 OK`**
```json
{
  "succeeded": true,
  "message": "Signal level entry retrieved successfully.",
  "data": {
    "id": "955aa4ac-b40c-4f18-84c6-a982d7c8b992",
    "signalLevelId": "5eb782e3-8edf-46fa-b0ea-f3ce0aa79b96",
    "color": "#1A7A1A",
    "thresholdFrom": -80,
    "thresholdTo": 0,
    "description": "Excellent",
    "sortOrder": 1
  },
  "errors": {}
}
```

**Not found `400 Bad Request`**
```json
{
  "succeeded": false,
  "message": "Signal level entry not found.",
  "data": null,
  "errors": {
    "Id": ["Signal level entry does not exist."]
  }
}
```

---

### PATCH `/signal-levels/{signalLevelId}/entries/{id}`
**Permission:** `signal_level_entries.update`

Partially updates an entry. Only send the fields you want to change.

**Request**
```json
PATCH /signal-levels/5eb782e3-8edf-46fa-b0ea-f3ce0aa79b96/entries/b9e0cf8b-c40a-4ce7-8dd9-9b937e62c90e
Content-Type: application/json

{
  "color": "#CC0000",
  "description": "Weak Signal"
}
```

**Success response `200 OK`**
```json
{
  "succeeded": true,
  "message": "Signal level entry updated successfully.",
  "data": {
    "id": "b9e0cf8b-c40a-4ce7-8dd9-9b937e62c90e",
    "signalLevelId": "5eb782e3-8edf-46fa-b0ea-f3ce0aa79b96",
    "color": "#CC0000",
    "thresholdFrom": -110,
    "thresholdTo": -99,
    "description": "Weak Signal",
    "sortOrder": 4
  },
  "errors": {}
}
```

**Duplicate error `400 Bad Request`** — same shape as POST duplicate error above.

---

### DELETE `/signal-levels/{signalLevelId}/entries/{id}`
**Permission:** `signal_level_entries.delete`

Soft-deletes an entry.

**Success response `200 OK`**
```json
{
  "succeeded": true,
  "message": "Signal level entry deleted successfully.",
  "data": true,
  "errors": {}
}
```

---

## Duplicate Validation

A signal level entry is considered a **duplicate** when another non-deleted entry under the same `signalLevelId` has an identical combination of:

| Field          | Enforced at         |
|----------------|---------------------|
| `signalLevelId`| App layer + Database |
| `thresholdFrom`| App layer + Database (see note below) |
| `thresholdTo`  | App layer + Database (see note below) |
| `sortOrder`    | App layer + Database |

> **Note on NULL thresholds:** The database unique index uses `NULLS NOT DISTINCT` (PostgreSQL 15+), which means two `NULL` values in `thresholdFrom` or `thresholdTo` are treated as equal for uniqueness purposes. Database enforcement is therefore complete for all field combinations, including those where `thresholdFrom` or `thresholdTo` is `NULL`.

This rule applies to both **create** and **update** operations.

### Frontend guidance

- **Disable the submit button** or show an inline warning when the user fills in a `(thresholdFrom, thresholdTo, sortOrder)` combination that already exists in the current list.
- Compare against the already-fetched entries list on the client side as a first pass (faster UX), then handle the `400` error from the API as the authoritative check.
- On a duplicate `400`, read `errors.SignalLevelEntry[0]` for the user-facing message.
- On a not-found `400`, read the first value of the `errors` object (key may vary: `Id`, `SignalLevelId`).

### Example client-side pre-check (JavaScript)

```js
function isDuplicate(entries, newEntry, excludeId = null) {
  return entries.some(e =>
    e.id !== excludeId &&
    e.thresholdFrom === newEntry.thresholdFrom &&
    e.thresholdTo   === newEntry.thresholdTo   &&
    e.sortOrder     === newEntry.sortOrder
  );
}
```
