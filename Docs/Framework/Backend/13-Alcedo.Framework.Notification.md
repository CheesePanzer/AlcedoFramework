# Alcedo.Framework.Notification

Notification routes **typed application messages** through database-configured templates to delivery consumers. The supplied delivery implementation sends email through MailKit/SMTP. The Contracts assembly also declares Sms and InApp channel values, but those dispatch branches are not implemented senders in the current module.

---

## Table of Contents

- [Registration and Configuration](#registration-and-configuration)
- [Declare a Message Contract](#declare-a-message-contract)
- [Configure Templates and Dispatch Rules](#configure-templates-and-dispatch-rules)
- [Publish a Notification](#publish-a-notification)
- [Test Rendering and Delivery](#test-rendering-and-delivery)
- [Errors Retries and Caches](#errors-retries-and-caches)
- [Troubleshooting and Further Reading](#troubleshooting-and-further-reading)

---

## Registration and Configuration

Register `.AddAlcedoFrameworkBase().AddAlcedoIdentity().AddAlcedoNotification()` in the host's module registry. Register the application module containing your payload contracts as well. The Notification module initializes `MessageTypeMetadataCenter` from registered assemblies and registers the template cache/email services; Core registers the consumers and transport.

The supplied configuration test-send service queries the current user through Identity's `IUserQueryIdentityService`. A host that does not use Identity must supply that contract and the required API authorization configuration itself.

Configure the SMTP sender and shared config-cache expiry:

```json
{
  "SmtpOptions": {
    "Host": "localhost",
    "Port": 25,
    "Username": "",
    "Password": "",
    "UseSsl": false,
    "FromEmail": "noreply@example.com",
    "FromName": "Alcedo"
  },
  "NotificationOptions": {
    "MessageCacheGlobalExpiryMins": 120
  }
}
```

The example targets a local SMTP capture server. `UseSsl=true` maps to MailKit `SecureSocketOptions.StartTls`; false uses Auto. This flag is not an explicit choice of implicit TLS on port 465. Select SMTP settings compatible with the implementation and provider, and supply credentials outside committed files.

Apply the application migrations and start the selected Core message transport. Registering the module does not create templates or a message-type configuration row automatically.

---

## Declare a Message Contract

Implement `INotificationMessage` and choose a stable message type code:

```csharp
using Alcedo.Framework.Notification.Contracts;

[MessageType("OrderPaid", "Order paid", "Payment confirmation")]
public sealed record OrderPaidPayload : INotificationMessage
{
    public string OrderNo { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}
```

`MessageTypeMetadataCenter` discovers concrete payload classes from registered module assemblies. The attribute overrides the default code/name; without it, the class name is used. Duplicate codes are rejected case-insensitively at initialization.

Initialize nested payload objects and collections. `MessageExampleGenerator` builds example JSON from payload types for administration; it is not a substitute for valid application data.

Inspect the discovered contracts through:

```text
GET /api/v1/notification/messagetypes
GET /api/v1/notification/messagetypes/{code}
```

These list code/name/description/example metadata, not database-created message-type entities. Keep codes stable when changing assembly or class names.

---

## Configure Templates and Dispatch Rules

Use the secured APIs or their application services in this order:

1. Create an Email template using `POST /api/v1/notification/messagetemplates`.
2. Enable it with `PATCH /api/v1/notification/messagetemplates/{id}/status:enable`.
3. Create a config with `POST /api/v1/notification/messageconfigs`, using the discovered MessageTypeCode.
4. Assign the template with `PATCH /api/v1/notification/messageconfigs/{id}/templates:assign` and a JSON GUID array.
5. Enable the config with `PATCH /api/v1/notification/messageconfigs/{id}/status:enable`.

Template creation body:

```json
{
  "code": "OrderPaidEmail",
  "name": "Order payment confirmation",
  "channel": "Email",
  "titleTemplate": "Payment received",
  "bodyTemplate": "Thank you for your order.",
  "externalTemplateId": null
}
```

Configuration creation body:

```json
{
  "code": "OrderPaidConfig",
  "name": "Order paid delivery",
  "messageTypeCode": "OrderPaid"
}
```

Both aggregates start in Draft and reuse Shared CRUD, Version, state, and audit conventions. A config needs at least one template before it can be enabled. Template assignment validates enabled templates through the domain service. Updates include the current DTO `version` along with the applicable update fields.

Templates use Scriban, with `MessageTemplateRenderHelper` deserializing the payload into `JsonElement` before rendering. Start with a render test against your actual payload and member names before adding dynamic expressions; neither payload validation nor this helper automatically supplies an application-specific template model. The plain-text templates above are a minimal delivery check.

`ExternalTemplateId` satisfies the template command's alternate validation branch, but the supplied SMTP sender does not call an external template-provider API. For the built-in Email path, configure an actual title/body rather than assuming that an external ID alone performs rendering.

---

## Publish a Notification

Application code can depend on the Contracts assembly and the Core unit of work:

```csharp
using Alcedo.Core.Application.Tools;
using Alcedo.Framework.Notification.Contracts;

public sealed class OrderNotificationPublisher(IUnitOfWorkExecutor executor)
{
    public Task PublishAsync(string orderNo, decimal amount, CancellationToken ct)
    {
        var payload = new OrderPaidPayload { OrderNo = orderNo, Amount = amount };
        var recipient = new RecipientContext(
            "Customer", Guid.NewGuid(), recipientType: "Customer",
            email: "customer@example.com");

        return executor.ExecuteAsync(() =>
        {
            executor.AddApplicationEvent(
                NotificationDispatchEvent.Create(payload, recipient));
        }, ct);
    }
}
```

This sample shows a standalone application operation. When an existing business unit of work already owns the transaction, add the event to that executor inside the existing operation rather than introducing an unnecessary nested unit of work.

The factory resolves the registered payload code and serializes with no property naming policy. The normal delivery path is:

```text
Application unit of work + Outbox
    -> NotificationDispatchEvent
    -> NotificationDispatchConsumer
    -> active config and templates
    -> render title/body
    -> NotificationEmailSendEvent
    -> NotificationEmailSender
    -> SMTP
```

An application commit or API success acknowledges submission, not confirmed mailbox delivery. The recipient's Email must be nonblank for email dispatch. `CanPhone`/`CanInApp` describe available recipient addresses, not proof that those channel senders are implemented.

---

## Test Rendering and Delivery

Two POST routes have different effects:

- `/api/v1/notification/messagetemplates/{id}/test` renders a template and returns title/body without sending email.
- `/api/v1/notification/messageconfigs/{id}/test` submits a real notification addressed to the current operator, using their available identity contact information.

Both controller actions bind **`[FromBody] string`**. Send a JSON string containing the payload JSON, not an unquoted JSON object:

```json
"{\"OrderNo\":\"ORDER-001\",\"Amount\":42}"
```

Supply Bearer authentication, `Content-Type: application/json`, and a fresh `X-Idempotency-Key` for each POST. For a safe delivery check, configure a local SMTP capture service and use an operator account with an email address.

The config supports a separate skip flag:

```text
PATCH /api/v1/notification/messageconfigs/{id}/skip:true
PATCH /api/v1/notification/messageconfigs/{id}/skip:false
GET   /api/v1/notification/messageconfigs/{id}/templates
```

Skip intentionally suppresses dispatch without changing the config's master-data status. Disabling and skipping are different operations.

---

## Errors Retries and Caches

Dispatch records conditions such as missing config, inactive template, missing email address, and rendering failure as `MessageErrorEntity`. Inspect and manage these through the singular controller prefix:

```text
GET    /api/v1/notification/messageerror
GET    /api/v1/notification/messageerror/{id}
PUT    /api/v1/notification/messageerror/{id}/event:retry
DELETE /api/v1/notification/messageerror/{id}
```

Retry reconstructs a dispatch event and removes the stored error within a unit of work. Delete ignores the error. SMTP exceptions are rethrown from the sender and follow the message transport's retry/fault handling; they are not automatically the same as rows in the dispatch-error API.

The current dispatch loop returns when one template/send-event cannot be prepared, so a mixed set of valid and invalid templates can suppress other deliveries in the same dispatch. Do not assume per-template partial success. Keep enabled email configs aligned with the implemented channels.

Config data uses Redis with `MessageCacheGlobalExpiryMins`. Parsed templates use a static in-process cache, refreshed by template events; the option name does not make that local dictionary expire. In a multi-instance deployment, account for refresh-event routing and stale local caches instead of assuming every instance observes an immediate broadcast invalidation.

---

## Troubleshooting and Further Reading

- **Message code not found**: register the payload's module before initialization; check spelling and duplicate codes.
- **No delivery after publishing**: check config/template status, assigned templates, skip flag, recipient Email, transport consumers, and SMTP logs.
- **Render test rejects the body**: wrap payload JSON in a JSON string because the current action binds `string`.
- **Test send has no recipient**: set contact information on the operator's Identity profile or use an application-supplied recipient with a valid email.
- **Template changed but output remains old**: inspect local cache refresh and worker instance routing.

See [Contracts](../../../Src/Framework/Backend/Alcedo.Framework.Notification.Contracts/Alcedo.Framework.Notification.Contracts), [Notification module](../../../Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/AlcedoNotificationModule.cs), [dispatch service](../../../Src/Framework/Backend/Alcedo.Framework.Notification/Alcedo.Framework.Notification/Application/Service/MessageDispatch/MessageDispatchApplicationService.cs), and [Core Transactions and Events](../../Core/Backend/02c-Core.Infrastructure-Transaction-Events.md).
