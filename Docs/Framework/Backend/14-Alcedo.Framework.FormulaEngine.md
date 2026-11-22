# Alcedo.Framework.FormulaEngine

FormulaEngine combines **typed input/output contracts** with database-managed expressions. Application modules declare formula classes; the seeder creates and validates their parameter structure; administrators edit expressions; application code executes them through `IFormulaEngine`.

---

## Table of Contents

- [Registration and Discovery](#registration-and-discovery)
- [Declare a Typed Formula](#declare-a-typed-formula)
- [Seed and Configure Expressions](#seed-and-configure-expressions)
- [Execute from Application Code](#execute-from-application-code)
- [Input Rules and Intermediate Variables](#input-rules-and-intermediate-variables)
- [Test through the API](#test-through-the-api)
- [Versioning and Contract Evolution](#versioning-and-contract-evolution)
- [Troubleshooting and Further Reading](#troubleshooting-and-further-reading)

---

## Registration and Discovery

Reference `Alcedo.Framework.FormulaEngine.Contract` from code that declares or invokes typed formulas. The host also needs the implementation project and Shared:

```csharp
registry.AddAlcedoFrameworkBase()
    .AddModule(new AlcedoFormulaEngineModule());
```

Register the application module that contains the formula class before host initialization. `FormulaStoreCenter.OnInitialize` scans registered module assemblies for concrete subclasses of `Formula<TInput,TOutput>`; a project reference alone does not register an assembly for scanning.

After applying database migrations, call `app.SeedFormulaEngine()`. Registration discovers metadata and registers `IFormulaEngine`; seeding persists missing definitions and validates existing parameter structure. Neither action supplies your business output expressions automatically. See the [host skeleton](00-Alcedo.Framework-Overview.md#register-the-host).

---

## Declare a Typed Formula

Place these types in a registered business module:

```csharp
using Alcedo.Framework.FormulaEngine.Contract.Abstraction;

[Formula("InvoiceTotal", "Invoice total", "Unit price multiplied by quantity")]
public sealed class InvoiceTotalFormula : Formula<InvoiceInput, InvoiceOutput>
{
}

public sealed class InvoiceInput
{
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public sealed class InvoiceOutput
{
    public decimal Total { get; set; }
}
```

Use a stable, unique formula code. `TInput` must be a class; `TOutput` must be a class with a parameterless constructor and writable output properties.

The flattener prefixes inputs with `In_` and joins nested property paths with underscores:

```text
InvoiceInput.UnitPrice -> In_UnitPrice
InvoiceInput.Quantity  -> In_Quantity
InvoiceOutput.Total    -> Total
Address.Postcode       -> In_Address_Postcode   (on an input DTO)
```

`[Param(variableName: "Price", description: "Unit price")]` can name a primitive input leaf. A complex object cannot have a VariableName override. Keep output names aligned with actual CLR property paths: the current writer does not reverse-map arbitrary output aliases. Avoid recursive DTOs and collections; flattening is designed around scalar leaves and nested objects.

---

## Seed and Configure Expressions

After seeding, query the plural controller route:

```text
GET /api/v1/formula-engine/formulas
GET /api/v1/formula-engine/formulas/{id}
```

Use the returned aggregate ID, parameter IDs, and current `version`. Newly seeded output parameters have empty expressions. Configure Total before execution:

```http
PATCH /api/v1/formula-engine/formulas/FORMULA_ID/outputparams
Authorization: Bearer ACCESS_TOKEN
Content-Type: application/json

{
  "formulaVersion": 1,
  "items": [
    {
      "outputParamId": "TOTAL_PARAMETER_ID",
      "expression": "In_UnitPrice * In_Quantity"
    }
  ]
}
```

The version and IDs above are illustrative; substitute the current DTO values. Each edit checks FormulaVersion and commits through a unit of work. Fetch the DTO again after editing, since later requests must use the updated version. Administrative routes inherit Framework RBAC authorization.

---

## Execute from Application Code

Inject `IFormulaEngine` into an application service:

```csharp
using Alcedo.Framework.FormulaEngine.Contract;

public sealed class InvoiceCalculator(IFormulaEngine engine)
{
    public async Task<decimal> CalculateAsync(CancellationToken ct)
    {
        var input = new InvoiceInput { UnitPrice = 12.5m, Quantity = 2 };
        var result = await engine.ExecuteAsync<InvoiceTotalFormula, InvoiceInput, InvoiceOutput>(input, ct);
        return result.Output.Total; // 25 when the configured expression is price * quantity
    }
}
```

`EngineResult<TOutput>` contains Output, TaskId, FormulaCode, and FormulaVersion. Retain the formula version with a business calculation when you need to explain which configured rule produced it.

The engine loads configuration by the registered code, flattens/validates input, evaluates intermediate expressions in ascending Order, evaluates outputs, and constructs the typed output. Execution logging includes serialized input/output. The presence of `FormulaExecutionLogEntity` does not mean every invocation automatically writes an execution-log row.

---

## Input Rules and Intermediate Variables

Edit input rules with `PATCH /api/v1/formula-engine/formulas/{id}/inputparams`:

```json
{
  "formulaVersion": 2,
  "items": [
    {
      "inputParamId": "UNIT_PRICE_PARAMETER_ID",
      "validCondition": "self >= 0",
      "defaultValue": null
    }
  ]
}
```

In a validation condition, `self` is the current leaf and other flattened inputs are available. The condition must evaluate to true. Validation runs before substituting a default for a null value, so a condition rejecting null is not rescued by a default.

The default converter recognizes decimal/int/string/bool/DateTime aliases, but generated metadata uses CLR type names. Not all names match (`Int32` versus `int`, for example). Verify nullable/default combinations before relying on them; non-null inputs do not take that conversion path.

Replace the intermediate expression list with `PATCH /api/v1/formula-engine/formulas/{id}/expressions`:

```json
{
  "formulaVersion": 3,
  "items": [
    { "order": 1, "varName": "Subtotal", "expression": "In_UnitPrice * In_Quantity", "note": null },
    { "order": 2, "varName": "GrandTotal", "expression": "Subtotal + 5", "note": "Delivery charge" }
  ]
}
```

Then update Total's output expression to `GrandTotal`, using the newly read version. This endpoint replaces the whole list, not one variable. Use unique names and distinct Order values for dependent steps. Output expressions can read inputs/intermediate variables; outputs are not added back to the intermediate context for dependencies between output properties.

---

## Test through the API

```text
GET  /api/v1/formula-engine/formulas/{id}/test   get an input JSON template
POST /api/v1/formula-engine/formulas/{id}/test   evaluate supplied input
```

The POST action binds `[FromBody] string`, so send a JSON string containing input JSON:

```http
POST /api/v1/formula-engine/formulas/FORMULA_ID/test
Authorization: Bearer ACCESS_TOKEN
Content-Type: application/json
X-Idempotency-Key: a-new-unique-request-key

"{\"UnitPrice\":12.5,\"Quantity\":2}"
```

The runner deserializes to the registered input type and invokes EngineCore through reflection. Its name “sandbox” describes a test helper; it does not establish a separate process or an untrusted-code security boundary. Use `IFormulaEngine` for business execution.

---

## Versioning and Contract Evolution

The version-specific overload takes the requested version and cancellation token:

```csharp
await engine.ExecuteAsync<InvoiceTotalFormula, InvoiceInput, InvoiceOutput>(
    input, version: 3, ct: ct);
```

It checks the current formula for that version, then `HistoryFormulaEntity` by Code/OriginalVersion. It fails when neither exists.

**An aggregate Version is not a complete historical revision store.** The edit service does not snapshot every expression edit. Requesting a previously observed version does not guarantee it remains executable. Define history retention before promising reproducible execution of every edit revision.

The seeder compares stored and declared input/output names/types. Changing structure under an existing code causes a startup error; introduce a new contract/code for a changed shape. Removing a class can trigger an orphan-code error. Although the source contains an obsolete/archive path, orphan validation runs first; marking a persisted definition obsolete is not an automatically safe migration either. Review the checks and plan data migration before retiring a definition, and do not reuse an archived code for a different contract.

---

## Troubleshooting and Further Reading

- **Formula type not found**: register its business module and the implementation module.
- **Database formula not found**: apply migrations and run the seeder after module initialization.
- **Empty/invalid expression**: configure output expressions after seeding and test representative inputs.
- **Version conflict**: reload the DTO; editing one section changes the version needed for subsequent sections.
- **Property not found during output construction**: align variables with writable CLR property paths.
- **Old version unavailable**: check whether a history row was retained; version increments alone do not create snapshots.

Source entry points: [module](../../../Src/Framework/Backend/Alcedo.Framework.FormulaEngine/AlcedoFormulaEngineModule.cs), [contracts](../../../Src/Framework/Backend/Alcedo.Framework.FormulaEngine.Contract), [seeder](../../../Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Infrastructure/Seeder/FormulaSeeder.cs), [controller](../../../Src/Framework/Backend/Alcedo.Framework.FormulaEngine/Controller/FormulaController.cs), and [engine](../../../Src/Framework/Backend/Alcedo.Framework.FormulaEngine/EngineCore/Core.cs). The [FormulaEngine tests](../../../Tests/Framework/Backend/Alcedo.Framework.FormulaEngine.Test) demonstrate isolated execution and parameter behaviors.
