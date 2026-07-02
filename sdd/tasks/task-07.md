# Task 07 — Testes Unitários

## Objetivo

Cobrir cada VO, invariantes de `Policy` e cada use case. **Meta**: 100% dos VOs e 100% dos use cases têm ≥ 1 happy path + 1 erro.

## Prerequisites

- `task-06` DONE.

## Files to create

### `tests/Segfy.Application.Tests/Fakes/`

- `InMemoryPolicyRepository.cs` — implementa `IPolicyRepository` com `List<Policy>` interno. Para `ListExpiringAsync`, filtro em memória (`Status == Ativa && CoverageEnd >= today && CoverageEnd <= today.AddDays(daysWindow)`), ordena por `CoverageEnd`. Suficiente para testes (a impl SQL cru é verificada manualmente no QA).
- `FakeClock.cs` — implementa `IClock`. `UtcNow`/`TodayUtc` settáveis via método `SetNow(DateTime)`.
- `FakePolicyNumberSequence.cs` — implementa `IPolicyNumberSequence`. `Dictionary<int, int>` interno. `NextForYearAsync(year)` incrementa e retorna.

### `tests/Segfy.Domain.Tests/Policies/ValueObjects/`

Um arquivo por VO. Cada método: `[Fact]` ou `[Theory]+[InlineData]`.

- `DocumentTests`:
  - `Create_WithValidCpf_ReturnsDigitsOnly`
  - `Create_WithValidCnpj_ReturnsDigitsOnly`
  - `Create_WithMask_StripsAndValidates`
  - `Create_WithInvalidCheckDigits_ThrowsDomainValidation`
  - `Create_WithRepeatingDigits_ThrowsDomainValidation`
  - `Create_WithWrongLength_ThrowsDomainValidation`
  - `LoadTrusted_BypassesValidation`

- `LicensePlateTests`:
  - `Create_WithOldFormatDashed_NormalizesToNoDash`
  - `Create_WithOldFormatNoDash_KeepsAsIs`
  - `Create_WithMercosulFormat_KeepsAsIs`
  - `Create_WithLowercase_Uppercases`
  - `Create_WithInvalidFormat_ThrowsDomainValidation`

- `PolicyNumberTests`:
  - `Create_ForYear2026Seq1_ReturnsSEG_2026_0001`
  - `Create_ForYear2026Seq9999_ReturnsSEG_2026_9999`
  - `Create_ForYear2026Seq10000_ExpandsPadding` (retorna `SEG-2026-10000`)
  - `Create_WithZeroOrNegativeSequential_ThrowsDomainValidation`
  - `Parse_ValidValue_RoundTrips`
  - `Parse_InvalidValue_ThrowsDomainValidation`

- `MoneyTests`:
  - `Create_WithPositive_Succeeds`
  - `Create_WithZero_ThrowsDomainValidation`
  - `Create_WithNegative_ThrowsDomainValidation`
  - `Create_WithThreeDecimals_RoundsToTwo`

- `CoveragePeriodTests`:
  - `Create_WithEndAfterStart_Succeeds`
  - `Create_WithEndEqualStart_ThrowsDomainValidation`
  - `Create_WithEndBeforeStart_ThrowsDomainValidation`

### `tests/Segfy.Domain.Tests/Policies/PolicyTests.cs`

- `Create_SetsStatusAtiva`
- `Create_SetsCreatedAndUpdatedAtToNowUtc`
- `ChangeStatus_AtivaToCancelada_Succeeds`
- `ChangeStatus_AtivaToExpirada_Succeeds`
- `ChangeStatus_CanceladaToAtiva_ThrowsInvalidState`
- `ChangeStatus_CanceladaToExpirada_ThrowsInvalidState`
- `ChangeStatus_ExpiradaToAtiva_ThrowsInvalidState`
- `ChangeStatus_AtivaToAtiva_ThrowsInvalidState`
- `UpdateDetails_UpdatesFieldsAndUpdatedAt`

### `tests/Segfy.Application.Tests/UseCases/`

Um arquivo por use case. Estilo AAA sem comentários.

- `CreatePolicyUseCaseTests`:
  - `Execute_WithValidInput_ReturnsPolicyWithGeneratedNumber`
  - `Execute_WithInvalidDocument_ThrowsDomainValidation`
  - `Execute_TwiceSameYear_IncrementsSequential`
- `GetPolicyByIdUseCaseTests`:
  - `Execute_ExistingId_ReturnsPolicy`
  - `Execute_UnknownId_ThrowsNotFound`
- `ListPoliciesUseCaseTests`:
  - `Execute_ReturnsPaginatedResult`
  - `Execute_EmptyRepository_ReturnsEmpty`
  - `Execute_ClampsPageSizeAbove100`
  - `Execute_ClampsPageBelow1`
- `UpdatePolicyUseCaseTests`:
  - `Execute_ValidUpdate_UpdatesFields`
  - `Execute_UnknownId_ThrowsNotFound`
  - `Execute_InvalidStatusTransition_ThrowsInvalidState`
- `DeletePolicyUseCaseTests`:
  - `Execute_ExistingId_RemovesPolicy`
  - `Execute_UnknownId_ThrowsNotFound`
- `GetExpiringPoliciesUseCaseTests`:
  - `Execute_ReturnsAtivaPoliciesWithinWindow`
  - `Execute_IgnoresCanceladaAndExpirada`
  - `Execute_IgnoresBeyondWindow`
  - `Execute_EmptyRepository_ReturnsEmpty`

## Files to modify

Nenhum de produção.

## Convenções

Ver `specs/coding-guidelines.md §Testes`. Sem `[Skip]`, sem `[Trait]` supérfluo.

## Acceptance criteria

- [ ] `dotnet test` verde.
- [ ] Tempo total < 5s.
- [ ] Cada VO tem ≥ 4 casos (válidos + inválidos).
- [ ] Cada use case tem ≥ 2 casos (happy + erro).

## Definition of Done

Ver template em `CLAUDE.md §Definition of Done`.
