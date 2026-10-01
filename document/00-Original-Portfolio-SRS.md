# SOFTWARE REQUIREMENTS SPECIFICATION

## Open-Source Project Portfolio

### Projects Covered

1. **DocuNest** — Personal & Family Document Vault  
2. **TaskFlow** — Simple Self-Hosted Task Manager  
3. **NotifyHub** — Universal Notification Gateway  
4. **LocalTools** — Privacy-First Browser Toolbox  
5. **DropBridge** — P2P File & Clipboard Sharing  

---

# 1. DOCUMENT INFORMATION

## 1.1 Purpose

This Software Requirements Specification defines the requirements, architecture expectations, system behavior, user roles, functional requirements, non-functional requirements, security requirements, APIs, data structures, deployment considerations, testing criteria, and future expansion opportunities for five independent open-source software projects.

Each project should:

- be usable independently;
- be deployable using Docker where applicable;
- prioritize privacy;
- support self-hosting where applicable;
- use open standards;
- expose documented APIs where useful;
- have clean, responsive user interfaces;
- support desktop and mobile browsers;
- be maintainable by a small development team;
- have an extensible architecture;
- provide clear contribution guidelines for the open-source community.

---

# 2. COMMON ENGINEERING PRINCIPLES

All server-based projects should follow these principles.

## 2.1 Open Source

Recommended license:

- AGPL-3.0 for server applications where hosted modifications should remain open;
- MIT or Apache-2.0 for libraries/client-only applications.

The final license may be selected separately per project.

## 2.2 Repository Standards

Each repository should contain:

- README.md
- LICENSE
- CONTRIBUTING.md
- CODE_OF_CONDUCT.md
- SECURITY.md
- CHANGELOG.md
- Dockerfile
- docker-compose.yml where applicable
- .env.example
- API documentation
- database migration instructions
- development setup
- production deployment instructions
- issue templates
- pull request template

## 2.3 Application Standards

Applications should provide:

- responsive design;
- dark/light theme where reasonable;
- keyboard accessibility;
- WCAG 2.1 AA target accessibility;
- localization-ready text;
- timezone awareness;
- structured logging;
- graceful error handling;
- health-check endpoints;
- version information;
- automated database migrations.

---

# PROJECT 1 — DOCUNEST

# 3. PRODUCT OVERVIEW

## 3.1 Product Name

**DocuNest**

## 3.2 Product Description

DocuNest is a privacy-focused document storage and organization application intended for individuals, families, freelancers, and small businesses.

The application allows users to upload, scan, categorize, search, organize, and monitor important documents.

Examples include:

- receipts;
- warranties;
- identity documents;
- contracts;
- bills;
- insurance documents;
- certificates;
- tax documents;
- invoices;
- vehicle documents;
- property documents.

The application should emphasize simplicity over enterprise document-management complexity.

## 3.3 Primary Value Proposition

> Scan it. Store it. Find it later.

## 3.4 Target Users

- individuals;
- families;
- freelancers;
- home offices;
- small businesses;
- accountants;
- consultants;
- self-hosting enthusiasts.

---

# 4. DOCUNEST USER ROLES

## 4.1 Administrator

Administrator can:

- configure the system;
- manage users;
- configure storage;
- manage OCR;
- configure email;
- configure backups;
- inspect logs;
- enable/disable registrations;
- configure authentication methods.

## 4.2 Owner

Owner can:

- create household/workspace;
- invite members;
- manage documents;
- configure categories;
- configure reminders;
- manage workspace settings.

## 4.3 Member

Member can:

- upload documents;
- view authorized documents;
- search documents;
- modify authorized documents;
- create reminders.

## 4.4 Read-Only Member

May:

- view permitted documents;
- search documents;
- download documents.

May not:

- delete;
- edit;
- upload.

---

# 5. DOCUNEST FUNCTIONAL REQUIREMENTS

## 5.1 Authentication

The system shall support:

- email/password login;
- logout;
- password reset;
- optional email verification;
- optional OAuth;
- optional OpenID Connect;
- optional LDAP in future versions;
- session management;
- device/session revocation.

### Security

Passwords shall be stored using:

- Argon2id preferred;
- bcrypt acceptable.

---

# 5.2 User Registration

Administrators shall be able to configure:

- open registration;
- invite-only registration;
- disabled registration.

---

# 5.3 Workspace Management

Users shall be able to create workspaces such as:

- Personal;
- Family;
- Business.

Each workspace shall contain independent:

- users;
- documents;
- tags;
- categories;
- reminders;
- permissions.

---

# 5.4 Document Upload

Supported upload mechanisms:

- drag and drop;
- file picker;
- mobile camera;
- clipboard paste;
- API upload;
- share-to application via PWA where supported.

Supported formats should include:

- PDF;
- JPEG;
- PNG;
- WebP;
- TIFF;
- HEIC where decoding support exists.

Future:

- DOCX;
- XLSX;
- email attachments.

---

# 5.5 Upload Validation

The system shall validate:

- supported extension;
- MIME type;
- maximum file size;
- malware scanning if enabled;
- duplicate hashes.

Administrator shall configure maximum upload size.

---

# 5.6 Document Processing Pipeline

After upload:

1. file shall be stored;
2. checksum shall be calculated;
3. thumbnail shall be generated;
4. text shall be extracted;
5. OCR shall run when needed;
6. metadata shall be extracted;
7. document classification may run;
8. suggested title shall be generated;
9. suggested date shall be generated;
10. suggested category shall be generated;
11. search index shall update.

Processing status values:

- uploaded;
- queued;
- processing;
- completed;
- failed.

---

# 5.7 OCR

OCR shall support:

- scanned PDFs;
- images;
- multi-page documents.

Recommended engines:

- Tesseract;
- PaddleOCR.

OCR language packs shall be configurable.

The application shall store:

- raw OCR text;
- normalized search text;
- OCR confidence where available.

---

# 5.8 Metadata Extraction

The application should attempt to identify:

- document title;
- document date;
- company/vendor;
- amount;
- currency;
- reference number;
- invoice number;
- warranty duration;
- expiration date.

Automatic metadata extraction shall remain editable by the user.

---

# 5.9 Document Viewer

Viewer shall provide:

- page navigation;
- zoom;
- rotate;
- fullscreen;
- download;
- metadata panel;
- OCR text panel.

---

# 5.10 Document Editing

Users shall be able to edit:

- title;
- category;
- tags;
- description;
- document date;
- issue date;
- expiration date;
- organization/vendor;
- amount;
- custom metadata.

---

# 5.11 Categories

Default categories may include:

- Identity;
- Financial;
- Insurance;
- Medical;
- Education;
- Vehicle;
- Property;
- Warranty;
- Receipt;
- Contract;
- Tax;
- Utility;
- Other.

Users shall be able to create custom categories.

---

# 5.12 Tags

Users shall be able to:

- create tags;
- assign multiple tags;
- rename tags;
- merge tags;
- delete tags.

---

# 5.13 Search

Search shall support:

- document title;
- OCR text;
- filename;
- tags;
- vendor;
- category;
- notes.

Filters:

- date;
- category;
- tags;
- owner;
- file type;
- expiration status;
- upload date.

---

# 5.14 Full-Text Search

Recommended engines:

Initial:

- PostgreSQL full-text search.

Optional:

- Meilisearch;
- Typesense;
- OpenSearch.

---

# 5.15 Reminders

Users shall be able to create reminders for:

- expiration;
- warranty;
- renewal;
- payment;
- custom date.

Notification intervals:

- same day;
- 1 day before;
- 7 days;
- 30 days;
- custom.

---

# 5.16 Dashboard

Dashboard should display:

- recent documents;
- documents expiring soon;
- warranties expiring soon;
- storage usage;
- recent activity;
- upload shortcut.

---

# 5.17 Duplicate Detection

Documents should be compared using SHA-256.

If duplicate is detected:

- warn user;
- offer to retain both;
- link to existing document;
- optionally cancel upload.

---

# 5.18 Document Sharing

Users may create temporary share links.

Settings:

- expiration;
- password protection;
- maximum downloads;
- disable download;
- revoke immediately.

---

# 5.19 Trash

Deleted documents shall enter Trash.

Trash retention:

- configurable;
- default 30 days.

Users may:

- restore;
- permanently delete.

---

# 5.20 Export

Users shall be able to export:

- individual document;
- selected documents;
- entire workspace.

Full export shall include:

- files;
- metadata;
- tags;
- categories;
- JSON manifest.

---

# 5.21 Import

Bulk import should support directory structures.

Optional import adapters may later support:

- Paperless;
- Google Drive;
- Dropbox;
- OneDrive.

---

# 6. DOCUNEST DATA MODEL

Core entities:

### User

- id
- email
- password_hash
- display_name
- created_at
- updated_at
- status

### Workspace

- id
- name
- owner_id
- created_at

### WorkspaceMember

- workspace_id
- user_id
- role

### Document

- id
- workspace_id
- filename
- original_filename
- storage_path
- mime_type
- file_size
- checksum
- title
- description
- category_id
- document_date
- expiration_date
- vendor
- amount
- currency
- processing_status
- created_by
- created_at
- updated_at

### OCRContent

- document_id
- raw_text
- normalized_text
- language
- confidence

### Category

- id
- workspace_id
- name

### Tag

- id
- workspace_id
- name

### DocumentTag

- document_id
- tag_id

### Reminder

- id
- document_id
- user_id
- remind_at
- channel
- status

### ShareLink

- id
- document_id
- token_hash
- expires_at
- download_limit
- download_count
- password_hash

---

# 7. DOCUNEST API

Base:

`/api/v1`

Endpoints should include:

### Authentication

- POST /auth/register
- POST /auth/login
- POST /auth/logout
- POST /auth/refresh
- POST /auth/reset-password

### Documents

- GET /documents
- GET /documents/{id}
- POST /documents
- PATCH /documents/{id}
- DELETE /documents/{id}
- POST /documents/{id}/restore

### Search

- GET /search

### Tags

- GET /tags
- POST /tags
- PATCH /tags/{id}
- DELETE /tags/{id}

### Categories

Equivalent CRUD endpoints.

### Reminders

Equivalent CRUD endpoints.

---

# 8. DOCUNEST NON-FUNCTIONAL REQUIREMENTS

## Performance

- dashboard target load under 2 seconds;
- search results under 1 second for normal datasets;
- uploads must support streaming;
- OCR shall operate asynchronously.

## Scalability

Initial expected deployment:

- 1–20 users;
- 10,000–100,000 documents.

Architecture should permit scaling beyond this.

## Security

Required:

- CSRF protection;
- XSS prevention;
- secure cookies;
- CSP headers;
- rate limiting;
- file validation;
- authorization enforcement;
- signed download URLs.

Optional:

- encrypted-at-rest file storage.

## Backups

Backup must include:

- database;
- uploaded files;
- configuration.

---

# 9. DOCUNEST ACCEPTANCE CRITERIA

MVP is complete when users can:

- register/login;
- create workspace;
- upload PDF/image;
- OCR uploaded documents;
- edit metadata;
- categorize documents;
- tag documents;
- search OCR text;
- create reminders;
- preview documents;
- download;
- delete/restore;
- deploy through Docker.

---

# PROJECT 2 — TASKFLOW

# 10. PRODUCT DESCRIPTION

TaskFlow is a lightweight, self-hostable task and project management application designed for individuals, families, and small teams.

Its main goal is speed and simplicity.

Primary design principle:

> Capture a task in seconds.

---

# 11. TASKFLOW USERS

Roles:

- System Administrator;
- Workspace Owner;
- Workspace Admin;
- Member;
- Guest.

---

# 12. TASKFLOW CORE FEATURES

## 12.1 Task Creation

Task shall support:

- title;
- description;
- project;
- section;
- due date;
- due time;
- priority;
- labels;
- assignee;
- recurrence;
- attachments;
- subtasks.

---

# 12.2 Quick Add

Example:

`Pay electricity bill tomorrow at 6pm #home !high`

Parser should detect:

- task text;
- date;
- time;
- project;
- priority.

---

# 12.3 Natural Language Parsing

Supported phrases should include:

- today;
- tomorrow;
- next Monday;
- Friday at 6 PM;
- every Monday;
- every 2 weeks;
- first day of every month.

Parsing should always display interpreted values before final creation when ambiguity exists.

---

# 12.4 Projects

Users shall be able to:

- create;
- rename;
- archive;
- delete;
- share projects.

---

# 12.5 Sections

Projects may contain sections:

Example:

Development

- Backlog
- In Progress
- Review
- Complete

---

# 12.6 Task Status

Default:

- open;
- completed.

Optional workflow:

- todo;
- in_progress;
- blocked;
- completed.

---

# 12.7 Priorities

Default:

- none;
- low;
- medium;
- high;
- urgent.

---

# 12.8 Labels

Users may assign multiple labels.

Examples:

- work;
- personal;
- phone;
- email;
- finance.

---

# 12.9 Recurring Tasks

Supported recurrence:

- daily;
- weekly;
- monthly;
- yearly;
- custom interval;
- selected weekdays.

System must calculate the next recurrence after completion.

---

# 12.10 Reminders

Notifications:

- browser;
- email;
- push;
- webhook.

Future integrations:

- Telegram;
- Slack;
- Discord.

---

# 12.11 Views

Required views:

- Inbox;
- Today;
- Upcoming;
- Overdue;
- Assigned to Me;
- Completed.

---

# 12.12 Kanban View

Optional project view:

Columns may represent:

- sections;
- statuses.

Support drag-and-drop.

---

# 12.13 List View

List should support:

- sorting;
- grouping;
- filters;
- bulk actions.

---

# 12.14 Calendar View

Display tasks based on due date.

Views:

- month;
- week.

---

# 12.15 Search

Search fields:

- task title;
- description;
- comments;
- labels.

Filters:

- project;
- assigned user;
- date;
- priority;
- completion state.

---

# 12.16 Collaboration

Members shall be able to:

- assign tasks;
- comment;
- mention users;
- upload attachments.

---

# 12.17 Activity History

Task activity shall record:

- creation;
- updates;
- assignments;
- completion;
- comments.

---

# 12.18 Keyboard Shortcuts

Examples:

- Q: Quick Add
- N: New Task
- /: Search
- E: Edit
- C: Complete
- Esc: Close

Shortcuts shall be configurable in the future.

---

# 13. TASKFLOW DATA MODEL

### Workspace

- id
- name

### User

- id
- email
- display_name

### Project

- id
- workspace_id
- name
- description
- archived

### Section

- id
- project_id
- name
- position

### Task

- id
- project_id
- section_id
- parent_task_id
- title
- description
- due_at
- priority
- status
- assigned_user_id
- created_by
- completed_at

### Recurrence

- task_id
- rule
- timezone

### Label

- id
- workspace_id
- name

### TaskLabel

- task_id
- label_id

### Comment

- id
- task_id
- user_id
- content

### Attachment

- id
- task_id
- file_path

### Activity

- id
- task_id
- event
- metadata

---

# 14. TASKFLOW API

Examples:

- GET /tasks
- POST /tasks
- GET /tasks/{id}
- PATCH /tasks/{id}
- DELETE /tasks/{id}
- POST /tasks/{id}/complete
- POST /tasks/{id}/comments

Projects:

- GET /projects
- POST /projects

Labels:

- GET /labels
- POST /labels

Search:

- GET /search/tasks

---

# 15. TASKFLOW NOTIFICATIONS

Notification event types:

- task assigned;
- reminder due;
- mention;
- comment;
- task overdue.

Users shall configure notification preferences independently.

---

# 16. TASKFLOW SECURITY

Implement:

- RBAC;
- rate limiting;
- secure authentication;
- attachment scanning;
- permissions at workspace/project level;
- activity auditing.

---

# 17. TASKFLOW ACCEPTANCE CRITERIA

MVP must support:

- authentication;
- workspaces;
- projects;
- quick task creation;
- due dates;
- priorities;
- recurring tasks;
- task completion;
- labels;
- reminders;
- Today view;
- Upcoming view;
- responsive interface;
- Docker deployment.

---

# PROJECT 3 — NOTIFYHUB

# 18. PRODUCT DESCRIPTION

NotifyHub is a universal notification router.

Applications send notifications using a single REST API.

NotifyHub forwards messages to configured services.

Example:

Application → NotifyHub → Telegram/Discord/Email/Web Push

---

# 19. SYSTEM GOALS

Developers should integrate NotifyHub using one request.

Example:

POST `/api/v1/messages`

```json
{
  "title": "Backup Failed",
  "message": "NAS backup failed",
  "priority": "critical",
  "topic": "servers"
}
```

---

# 20. NOTIFYHUB USERS

Roles:

- Administrator;
- Workspace Owner;
- Developer;
- Read-only User.

---

# 21. DESTINATION PROVIDERS

MVP providers:

- Email;
- Telegram;
- Discord;
- generic webhook;
- Web Push.

Future:

- Slack;
- Microsoft Teams;
- Matrix;
- Signal integrations where technically feasible;
- ntfy;
- Gotify;
- Pushover.

---

# 22. NOTIFYHUB APPLICATIONS

Users shall create applications.

Example:

- Production Server;
- Home Assistant;
- Shopify Store;
- Backup Server.

Each application receives credentials.

---

# 23. API KEYS

Users shall be able to:

- generate keys;
- name keys;
- revoke keys;
- rotate keys;
- restrict key permissions.

Raw secret shall only be shown once.

Database should retain only secure hashes where practical.

---

# 24. TOPICS

Notifications may belong to topics.

Examples:

- servers;
- payments;
- backups;
- security;
- deployments.

---

# 25. PRIORITIES

Supported:

- low;
- normal;
- high;
- critical.

---

# 26. ROUTING RULES

Example:

IF:

`priority = critical`

THEN:

- Telegram;
- Email;
- Web Push.

Rules may inspect:

- application;
- topic;
- priority;
- title;
- tags.

---

# 27. QUIET HOURS

Users may configure quiet periods.

Example:

22:00–07:00.

Critical notifications may optionally bypass quiet hours.

---

# 28. RATE LIMITING

Limits may apply:

- per API key;
- per application;
- per IP;
- per workspace.

When exceeded return HTTP 429.

---

# 29. RETRY SYSTEM

If provider fails:

1. initial attempt;
2. retry;
3. exponential backoff;
4. dead-letter state.

Administrators shall view failures.

---

# 30. MESSAGE STATUS

States:

- accepted;
- queued;
- processing;
- delivered;
- partially_delivered;
- failed.

---

# 31. MESSAGE HISTORY

Dashboard shall display:

- timestamp;
- source;
- destination;
- priority;
- delivery result;
- retries.

---

# 32. MESSAGE RETENTION

Administrator configures retention.

Examples:

- 7 days;
- 30 days;
- 90 days;
- forever.

Sensitive message payload logging may be disabled.

---

# 33. MESSAGE TEMPLATES

Users may create templates.

Variables:

`{{server_name}}`

`{{timestamp}}`

`{{error}}`

---

# 34. WEBHOOK DESTINATION

Configuration:

- URL;
- HTTP method;
- headers;
- authentication;
- timeout.

Optional HMAC signature shall verify message authenticity.

---

# 35. INBOUND WEBHOOK

NotifyHub shall optionally receive third-party webhook formats.

Examples:

- GitHub;
- Stripe;
- Uptime Kuma.

Adapters may convert them into internal message objects.

---

# 36. NOTIFYHUB DATA MODEL

### Workspace

### User

### Application

- id
- workspace_id
- name

### APIKey

- id
- application_id
- key_hash
- permissions
- last_used_at

### Provider

- id
- workspace_id
- type
- encrypted_config

### RoutingRule

- id
- workspace_id
- conditions
- destinations
- enabled
- priority

### Message

- id
- application_id
- title
- body
- priority
- topic
- metadata
- created_at

### Delivery

- id
- message_id
- provider_id
- status
- attempts
- last_error

---

# 37. NOTIFYHUB API

POST `/api/v1/messages`

GET `/api/v1/messages/{id}`

GET `/api/v1/messages`

POST `/api/v1/applications`

POST `/api/v1/api-keys`

POST `/api/v1/providers`

POST `/api/v1/rules`

---

# 38. NOTIFYHUB WEB DASHBOARD

Pages:

- Overview;
- Applications;
- API Keys;
- Destinations;
- Routing Rules;
- Message History;
- Failed Deliveries;
- Settings;
- System Health.

---

# 39. SECURITY

Provider credentials shall be encrypted.

Recommended:

AES-256-GCM using a deployment master key.

Additional requirements:

- HTTPS;
- audit logging;
- rate limiting;
- API key scopes;
- secret redaction;
- CSRF;
- CSP;
- SSRF protections for webhook URLs.

---

# 40. OBSERVABILITY

Expose metrics such as:

- messages received;
- messages delivered;
- provider failures;
- queue depth;
- average delivery latency.

Optional Prometheus endpoint:

`/metrics`

---

# 41. NOTIFYHUB ACCEPTANCE CRITERIA

MVP must:

- create application;
- generate API key;
- accept notification API;
- connect Telegram;
- connect Discord;
- send email;
- use generic webhook;
- configure routing rules;
- retry failures;
- show delivery logs;
- run through Docker.

---

# PROJECT 4 — LOCALTOOLS

# 42. PRODUCT DESCRIPTION

LocalTools is a privacy-first web application containing common file, image, text, developer, and media utilities.

Primary rule:

> User files should remain on the user's device whenever technically possible.

Most operations shall run inside the browser.

---

# 43. PRODUCT ARCHITECTURE

LocalTools should preferably consist of:

- static frontend;
- WebAssembly modules;
- Web Workers;
- IndexedDB;
- Service Worker.

No server should be required for normal processing.

---

# 44. TOOL CATEGORIES

## Image Tools

- image resize;
- crop;
- compress;
- rotate;
- format conversion;
- metadata removal;
- WebP converter;
- AVIF converter;
- SVG optimizer.

## Text Tools

- text comparison;
- case conversion;
- whitespace cleanup;
- word counter;
- character counter;
- Base64 encode/decode;
- URL encode/decode.

## Developer Tools

- JSON formatter;
- JSON validator;
- YAML formatter;
- XML formatter;
- JWT decoder;
- UUID generator;
- hashing;
- regex tester.

## File Tools

- create ZIP;
- extract ZIP;
- SHA checksum;
- filename cleaner;
- duplicate hash comparison.

## Media Tools

Using FFmpeg WASM:

- video → audio;
- trim video;
- trim audio;
- change video container;
- GIF creation;
- extract frames.

---

# 45. PRIVACY REQUIREMENTS

The interface shall clearly indicate:

`Processed locally in your browser.`

The application shall not upload files unless a particular tool explicitly requires cloud processing.

For MVP, avoid all cloud-dependent tools.

---

# 46. OFFLINE SUPPORT

LocalTools should operate as a PWA.

After required modules have been cached, supported tools should operate without internet.

---

# 47. FILE SIZE MANAGEMENT

Browser limitations should be communicated.

The application shall:

- display file size before processing;
- warn before processing very large files;
- release memory after operation;
- use streaming APIs where possible.

---

# 48. WEB WORKERS

CPU-intensive operations shall run through workers so the interface remains responsive.

---

# 49. PROGRESS REPORTING

Long operations shall display:

- percentage;
- current stage;
- cancel control.

---

# 50. LOCALTOOLS HISTORY

Privacy-friendly default:

No operation history.

Optional:

Local-only history using IndexedDB.

Users must be able to clear it.

---

# 51. TOOL FAVORITES

Users may mark favorite tools.

Favorites shall persist locally.

---

# 52. SEARCH

Homepage shall include tool search.

Examples:

Search:

`jpg`

Results:

- JPG → PNG
- JPG compressor
- JPG resize

---

# 53. DRAG AND DROP

File-based tools shall support drag-and-drop.

---

# 54. BATCH PROCESSING

Where technically reasonable:

- multiple image resize;
- multiple compression;
- bulk format conversion;
- ZIP generation.

---

# 55. DOWNLOAD

Processed files shall be downloadable immediately using browser-generated blobs.

No server storage.

---

# 56. SHAREABLE TOOL URLs

Each tool shall have a unique route.

Example:

`/tools/json-formatter`

This benefits:

- bookmarks;
- SEO;
- sharing.

---

# 57. LOCALTOOLS ACCESSIBILITY

Required:

- keyboard navigation;
- screen reader labels;
- visible focus indicators;
- accessible error messages;
- sufficient contrast.

---

# 58. LOCALTOOLS SECURITY

Because user-provided files are processed:

- avoid executing uploaded scripts;
- sanitize SVG;
- use sandboxed previews;
- apply CSP;
- isolate parsers where feasible;
- keep third-party libraries current.

---

# 59. PERFORMANCE REQUIREMENTS

Initial application:

- first useful paint under 2 seconds on typical broadband;
- lazy-load heavy libraries;
- load FFmpeg only on media tools;
- cache WASM bundles.

---

# 60. ANALYTICS

If analytics are provided:

- disabled by default in self-hosted builds;
- privacy-focused;
- no file information;
- no filename collection;
- no file content collection.

---

# 61. LOCALTOOLS ACCEPTANCE CRITERIA

MVP should contain at least:

- image resize;
- image compression;
- image format converter;
- metadata remover;
- JSON formatter;
- JSON validator;
- Base64;
- URL encoding;
- file hashing;
- ZIP creation;
- ZIP extraction;
- text diff.

All operations should function locally.

---

# PROJECT 5 — DROPBRIDGE

# 62. PRODUCT DESCRIPTION

DropBridge is a privacy-focused device-to-device sharing application using peer-to-peer communication.

Users shall transfer:

- files;
- text;
- URLs;
- clipboard content.

Primary transport:

WebRTC.

---

# 63. CORE PRINCIPLE

Where possible:

Files shall travel:

`Device A → Device B`

not:

`Device A → Cloud Storage → Device B`

A signaling service may coordinate peer connections.

---

# 64. DEVICE DISCOVERY

Users shall be able to discover devices through:

- local network session;
- temporary room;
- QR pairing;
- pairing code.

---

# 65. DEVICE IDENTITY

Each browser installation may generate:

- local device ID;
- cryptographic key pair;
- display name.

Example:

`Saif's Desktop`

---

# 66. DEVICE PAIRING

Persistent pairing flow:

1. desktop requests pairing;
2. QR code appears;
3. phone scans;
4. device fingerprints are exchanged;
5. user confirms;
6. devices become trusted.

---

# 67. TRANSFER TYPES

Supported:

- single file;
- multiple files;
- folder where browser support permits;
- image;
- text;
- URL.

---

# 68. CLIPBOARD SHARE

Users may send:

- copied text;
- URL;
- snippets.

Automatic clipboard synchronization should be optional and disabled by default for privacy reasons.

---

# 69. FILE TRANSFER FLOW

1. sender selects recipient;
2. selects file;
3. receiver receives transfer request;
4. receiver accepts;
5. WebRTC data channel opens;
6. chunks transfer;
7. receiver reassembles file;
8. checksum verifies integrity;
9. download becomes available.

---

# 70. TRANSFER CHUNKING

Large files shall use chunking.

Recommended configurable chunk size.

Each transfer shall track:

- total bytes;
- sent bytes;
- percentage;
- throughput;
- estimated remaining duration where practical.

---

# 71. FILE INTEGRITY

Generate SHA-256 checksum.

Receiver shall verify final file hash.

---

# 72. ENCRYPTION

WebRTC provides transport encryption.

Additional application-level end-to-end encryption may optionally be introduced for relay-based fallback.

---

# 73. SIGNALING SERVER

Responsibilities:

- room creation;
- peer discovery;
- offer exchange;
- answer exchange;
- ICE candidate exchange.

The signaling server shall not normally receive file content.

---

# 74. STUN/TURN

System shall support configurable:

- STUN servers;
- TURN servers.

Default deployment may use public STUN for development.

Production users should configure their own TURN.

---

# 75. RELAY FALLBACK

When direct P2P fails, optional relay functionality may be supported.

Relay mode shall:

- clearly indicate relay use;
- retain encryption;
- avoid persistent storage;
- delete temporary chunks immediately.

---

# 76. TEMPORARY ROOMS

User may create a room.

Example code:

`482-913`

Room may expire after:

- 10 minutes;
- configurable timeout.

---

# 77. QR CODES

QR shall encode:

- room ID;
- pairing token;
- server origin.

Tokens must have expiration.

---

# 78. TRUSTED DEVICES

Users may manage trusted devices.

Actions:

- rename;
- revoke;
- block;
- forget.

---

# 79. TRANSFER HISTORY

Default preference:

Local-only history.

Fields:

- file name;
- size;
- sender;
- recipient;
- timestamp;
- status.

User may disable history entirely.

---

# 80. DEVICE PRESENCE

Trusted devices may display:

- online;
- offline;
- last seen.

Presence may be optional for privacy.

---

# 81. NOTIFICATIONS

Browser notification examples:

- incoming file;
- pairing request;
- transfer completed;
- transfer failed.

---

# 82. TRANSFER CONTROLS

Users shall have:

- pause where supported;
- cancel;
- reject;
- retry.

Resume may be introduced later.

---

# 83. DROPBRIDGE DATA MODEL

Most data may remain client-side.

Server entities:

### Room

- room_id
- expires_at

### PeerSession

- peer_id
- connection_id
- created_at

### SignalingMessage

Ephemeral only.

Persistent account mode, if added later:

### User

### Device

### TrustedDeviceRelationship

No file content should be persisted by default.

---

# 84. SIGNALING API

Potential WebSocket events:

- join_room
- peer_joined
- offer
- answer
- ice_candidate
- peer_left
- transfer_metadata

---

# 85. P2P PROTOCOL

Transfer metadata:

```json
{
  "transferId": "uuid",
  "type": "file",
  "name": "photo.jpg",
  "mime": "image/jpeg",
  "size": 1024000,
  "sha256": "..."
}
```

Message types:

- transfer_offer;
- transfer_accept;
- transfer_reject;
- chunk;
- progress;
- transfer_complete;
- transfer_cancel.

---

# 86. DROPBRIDGE SECURITY

Must protect against:

- unauthorized room access;
- token guessing;
- replay attacks;
- malicious filenames;
- file path traversal;
- oversized payload attacks;
- WebSocket abuse;
- denial of service.

Controls:

- random room tokens;
- rate limiting;
- expiring sessions;
- secure WebSockets;
- filename sanitization;
- CSP;
- input validation.

---

# 87. DROPBRIDGE ACCEPTANCE CRITERIA

MVP is complete when:

- two browser devices can discover each other;
- users can create/join room;
- QR join works;
- peer connection establishes;
- single file transfers;
- multiple files transfer;
- text transfer works;
- URL transfer works;
- progress is displayed;
- transfer can be rejected;
- SHA-256 is verified;
- signaling server runs with Docker.

---

# 88. COMMON UI/UX REQUIREMENTS

All five applications should have:

- desktop responsive UI;
- tablet responsive UI;
- mobile responsive UI;
- consistent loading indicators;
- meaningful empty states;
- actionable error messages;
- keyboard navigation;
- dark/light themes where practical.

---

# 89. INTERNATIONALIZATION

Applications should be structured for translation.

Requirements:

- all interface strings externalized;
- UTF-8 everywhere;
- RTL layout support should be possible;
- localized dates;
- localized time;
- localized number formatting.

Possible early languages:

- English;
- Urdu;
- Spanish;
- German;
- French.

English should be the initial default.

---

# 90. TIMEZONE REQUIREMENTS

Store timestamps in UTC.

Display timestamps according to user's configured timezone.

Recurring schedules must preserve intended local time across daylight-saving changes.

---

# 91. COMMON SECURITY REQUIREMENTS

Server-based projects shall follow OWASP recommendations.

Required:

- HTTPS;
- secure headers;
- Content Security Policy;
- secure cookie attributes;
- authorization on every protected action;
- server-side input validation;
- output escaping;
- rate limiting;
- brute-force protection;
- secret redaction;
- dependency vulnerability scanning;
- SQL parameterization;
- audit logging for sensitive changes.

---

# 92. SECRET MANAGEMENT

Secrets shall never be committed.

Configuration shall use environment variables or supported secret stores.

Example:

```env
DATABASE_URL=
APP_SECRET=
SMTP_PASSWORD=
ENCRYPTION_KEY=
```

---

# 93. DATABASE REQUIREMENTS

Recommended default database:

**PostgreSQL**

SQLite may be supported for:

- development;
- single-user installations;
- small deployments.

Database changes shall use migrations.

Migration tools may include:

- Prisma;
- Drizzle;
- Alembic;
- Flyway;
- Liquibase.

---

# 94. BACKGROUND JOBS

DocuNest and NotifyHub require asynchronous workers.

Candidates:

- Redis + BullMQ;
- Celery;
- RabbitMQ;
- PostgreSQL-backed job queue.

Workers must support:

- retry;
- failure handling;
- job status;
- idempotency.

---

# 95. LOGGING

Logs should use structured JSON in production.

Levels:

- debug;
- info;
- warning;
- error;
- critical.

Sensitive values shall be redacted.

---

# 96. ERROR HANDLING

API errors should use consistent structure.

Example:

```json
{
  "error": {
    "code": "DOCUMENT_NOT_FOUND",
    "message": "The requested document could not be found.",
    "requestId": "..."
  }
}
```

---

# 97. OBSERVABILITY

Server applications should expose:

`GET /health`

Response shall confirm basic application health.

Optional endpoints:

- `/ready`
- `/metrics`

---

# 98. API VERSIONING

Use:

`/api/v1`

Breaking API changes should create new versions.

---

# 99. API DOCUMENTATION

Recommended:

OpenAPI 3.x.

Swagger UI may be served at:

`/api/docs`

Documentation should include:

- authentication;
- parameters;
- request schemas;
- response schemas;
- error responses;
- examples.

---

# 100. RATE LIMITING

Rate limits should be configurable.

Rules may include:

- anonymous requests;
- authenticated requests;
- login;
- API key usage;
- expensive operations.

---

# 101. FILE STORAGE

Projects using file storage should abstract the storage driver.

Drivers:

MVP:

- local filesystem.

Future:

- S3;
- MinIO;
- Cloudflare R2;
- Backblaze B2;
- Azure Blob.

---

# 102. CONTAINERIZATION

Server-based projects shall provide Docker images.

Example installation:

```bash
docker compose up -d
```

Docker Compose should manage:

- application;
- database;
- optional Redis;
- worker.

---

# 103. CONFIGURATION

Use `.env`.

Provide `.env.example`.

Configuration values should include:

- application URL;
- database URL;
- registration mode;
- storage path;
- email settings;
- log level.

---

# 104. DEPLOYMENT

Supported deployment targets should include:

- Docker Compose;
- Linux VPS;
- home servers;
- NAS;
- Proxmox containers/VMs.

Potential documentation:

- Unraid;
- TrueNAS;
- Synology;
- CasaOS.

---

# 105. UPDATE STRATEGY

Applications shall support semantic versioning.

Example:

`v1.4.2`

Use:

MAJOR.MINOR.PATCH

Database migrations should execute safely during upgrades.

---

# 106. BACKUP & RESTORE

Self-hosted applications should provide documentation for:

- database backup;
- file backup;
- restoration;
- migration to another server.

Where reasonable, applications may provide built-in export.

---

# 107. TELEMETRY

Open-source self-hosted versions should not require telemetry.

If telemetry exists:

- opt-in only;
- disclose exact information collected;
- never collect document/file contents;
- never collect credentials.

---

# 108. TESTING REQUIREMENTS

Each repository should include automated tests.

## Unit Tests

Test:

- business rules;
- parsers;
- permission checks;
- transformations.

## Integration Tests

Test:

- database;
- API;
- storage;
- external integrations.

## End-to-End Tests

Recommended framework:

- Playwright.

Critical user journeys should be tested.

---

# 109. SECURITY TESTING

Include:

- dependency scanning;
- secret scanning;
- static code analysis;
- authentication tests;
- authorization tests;
- input validation tests.

GitHub Actions may run:

- tests;
- lint;
- formatting;
- build;
- security scan.

---

# 110. CI/CD

Suggested pipeline:

Push

↓

Lint

↓

Unit Tests

↓

Integration Tests

↓

Build

↓

Security Scan

↓

Docker Build

↓

Release

---

# 111. RELEASE ARTIFACTS

GitHub releases may contain:

- source archive;
- changelog;
- Docker image reference;
- migration notes.

---

# 112. DOCUMENTATION

Every project should contain:

## User Documentation

- installation;
- getting started;
- features;
- FAQ;
- backup;
- upgrade;
- troubleshooting.

## Developer Documentation

- architecture;
- project structure;
- development setup;
- tests;
- database;
- API;
- contribution guidelines.

---

# 113. ACCESSIBILITY

Target:

WCAG 2.1 AA.

Key requirements:

- semantic HTML;
- ARIA only where necessary;
- keyboard accessible controls;
- no keyboard traps;
- appropriate contrast;
- screen reader labels;
- error identification.

---

# 114. BROWSER SUPPORT

Target current versions of:

- Chrome;
- Edge;
- Firefox;
- Safari.

PWA features may vary based on browser capabilities.

---

# 115. PERFORMANCE TARGETS

General frontend goals:

- Lighthouse Performance target ≥ 85;
- Lighthouse Accessibility target ≥ 90;
- avoid blocking scripts;
- lazy-load large modules.

Server API:

Typical CRUD request target:

<500 ms under normal single-node load.

---

# 116. OPEN-SOURCE CONTRIBUTION MODEL

Repository should label issues:

- good first issue;
- help wanted;
- bug;
- enhancement;
- documentation;
- security.

Provide development instructions allowing contributors to launch the project in minimal commands.

Example:

```bash
git clone ...
cp .env.example .env
docker compose up
```

---

# 117. PROJECT ROADMAP RECOMMENDATION

## Phase 1 — MVP

Build only essential functionality.

## Phase 2 — Polish

- accessibility;
- mobile experience;
- import/export;
- performance;
- themes.

## Phase 3 — Integrations

Add external services and plugin interfaces.

## Phase 4 — Ecosystem

Add:

- extensions;
- API clients;
- templates;
- community integrations.

---

# 118. RECOMMENDED DEVELOPMENT ORDER

For a small team, suggested order is:

### 1. NotifyHub

Smallest architecture and excellent API-focused project.

### 2. TaskFlow

Good opportunity to establish shared auth/workspace infrastructure.

### 3. DocuNest

More complex due to OCR and storage.

### 4. LocalTools

Independent client-side project.

### 5. DropBridge

Requires deeper networking/WebRTC engineering.

The projects do not need to be built in this order and should remain independent repositories.

---

# 119. POSSIBLE SHARED LIBRARIES

If the same team develops multiple projects, reusable libraries may include:

### UI package

- buttons;
- dialogs;
- forms;
- tables;
- notifications.

### Authentication package

- session handling;
- OAuth;
- RBAC.

### Common utility package

- logging;
- validation;
- error handling.

Avoid creating tight cross-project dependencies that make independent installation difficult.

---

# 120. RECOMMENDED TECHNICAL STACK

A practical standardized stack for the server projects:

### Frontend

- Next.js;
- React;
- TypeScript;
- Tailwind CSS;
- shadcn/ui or a custom component system.

### Backend

Option A:

- Next.js API/server;
- TypeScript.

Option B:

- FastAPI;
- Python.

For DocuNest, Python is especially useful because of OCR/document processing.

### Database

- PostgreSQL.

### ORM

TypeScript:

- Prisma;
- Drizzle.

Python:

- SQLAlchemy.

### Queue

- Redis;
- BullMQ/Celery.

### Authentication

- custom secure session implementation;
- Auth.js;
- OIDC provider support.

---

# 121. SUGGESTED PROJECT-SPECIFIC STACKS

## DocuNest

Frontend:

Next.js + TypeScript

Backend:

FastAPI

Database:

PostgreSQL

OCR:

Tesseract/PaddleOCR

Workers:

Celery + Redis

Storage:

Filesystem/S3-compatible abstraction

---

## TaskFlow

Frontend + Backend:

Next.js + TypeScript

Database:

PostgreSQL

ORM:

Drizzle/Prisma

Realtime:

WebSockets/SSE

---

## NotifyHub

Recommended backend:

Go

Alternative:

Node.js/TypeScript

Database:

PostgreSQL

Queue:

Redis or PostgreSQL queue

Frontend:

React/Next.js

---

## LocalTools

Frontend:

React/Next.js/Vite

Processing:

WebAssembly

Media:

FFmpeg WASM

Persistence:

IndexedDB

Server:

Not required for core features

---

## DropBridge

Frontend:

React/TypeScript

Networking:

WebRTC

Signaling:

Node.js/Go WebSocket server

TURN:

coturn

Optional PWA:

yes

---

# 122. INITIAL REPOSITORY STRUCTURE

Example server-based repository:

```text
project/
│
├── apps/
│   ├── web/
│   ├── api/
│   └── worker/
│
├── packages/
│   ├── ui/
│   ├── database/
│   └── shared/
│
├── docker/
│
├── docs/
│
├── tests/
│
├── scripts/
│
├── .github/
│   └── workflows/
│
├── docker-compose.yml
├── README.md
├── LICENSE
└── CONTRIBUTING.md
```

Small projects should avoid unnecessary monorepo complexity.

---

# 123. DEFINITION OF DONE

A feature is considered complete when:

- implementation is finished;
- input validation exists;
- permissions are enforced;
- errors are handled;
- UI works on desktop;
- UI works on mobile;
- automated tests cover critical logic;
- documentation is updated;
- API documentation is updated where relevant;
- no known critical security issue exists;
- code passes CI.

---

# 124. PRODUCT SUCCESS METRICS

Because these are open-source projects, useful indicators include:

- GitHub stars;
- forks;
- contributors;
- Docker pulls;
- active installations where voluntarily reported;
- issue resolution time;
- returning contributors;
- release adoption;
- documentation traffic.

Product telemetry shall not be required to use the applications.

---

# 125. OUT-OF-SCOPE ITEMS FOR INITIAL MVP

To prevent feature creep, initial versions should generally avoid:

- native iOS apps;
- native Android apps;
- enterprise SSO;
- enterprise audit compliance;
- multi-region clusters;
- Kubernetes requirement;
- complex billing;
- AI agents;
- marketplace systems;
- advanced analytics;
- enterprise workflow builders.

These may be introduced after demand is validated.

---

# 126. FINAL MVP SCOPE SUMMARY

## DocuNest v1

Must provide:

- authentication;
- document uploads;
- OCR;
- metadata editing;
- categories;
- tags;
- full-text search;
- reminders;
- document preview;
- workspace support;
- Docker deployment.

## TaskFlow v1

Must provide:

- accounts;
- projects;
- tasks;
- natural-language quick add;
- due dates;
- priorities;
- labels;
- recurring tasks;
- reminders;
- Today/Upcoming views;
- Docker deployment.

## NotifyHub v1

Must provide:

- applications;
- API keys;
- REST notification API;
- Telegram;
- Discord;
- Email;
- generic webhook;
- routing;
- retries;
- message history;
- Docker deployment.

## LocalTools v1

Must provide:

- browser-only processing;
- image tools;
- text tools;
- developer tools;
- hashing;
- archive tools;
- offline-capable PWA;
- zero mandatory backend.

## DropBridge v1

Must provide:

- peer discovery;
- rooms;
- QR joining;
- WebRTC connection;
- file transfer;
- text transfer;
- URL transfer;
- integrity verification;
- progress;
- cancel/reject;
- Dockerized signaling server.

---

# 127. FINAL PRODUCT PRINCIPLES

Every project should remain:

**Simple enough for a new user.**

**Powerful enough for technical users.**

**Private by default.**

**Self-hostable where applicable.**

**Easy to install.**

**Easy to contribute to.**

**API-friendly.**

**Well documented.**

**Free from unnecessary feature bloat.**

The strongest competitive advantage for these projects should not be having more features than existing applications.

It should be:

> fewer features, implemented exceptionally well.