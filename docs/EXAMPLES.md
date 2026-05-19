# Real-World Examples from allitude-pms-service & allitude-pms-fe

Live code examples from the Allitude PMS projects.

---

## Backend: Controller Registration

**File**: `allitude-pms-service/server/Api/Index.cs`

```cs
// Generic controller registration
public partial class Dora : EntityController<PMS_DORA>
{
    public new DbContext ctx;
    public Dora(DbContext context, IConfiguration configuration)
        : base(context, configuration)
    {
        this.ctx = context;
    }
}

public partial class MacroDati : EntityController<PMS_MACRODATI>
{
    public new DbContext ctx;
    public MacroDati(DbContext context, IConfiguration configuration)
        : base(context, configuration)
    {
        this.ctx = context;
    }
}

public partial class Cla : EntityController<CLA_CLA_PMS>
{
    public new DbContext ctx;
    public Cla(DbContext context, IConfiguration configuration)
        : base(context, configuration)
    {
        this.ctx = context;
    }
}
```

---

## Backend: Custom GetPage with Complex Filtering

**File**: `allitude-pms-service/server/Api/MacroDati/GetRelatedPage.cs`

```cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ECFramework;

public partial class MacroDati : EntityController<PMS_MACRODATI>
{
    [HttpPost("GetRelatedPage")]
    public Task<Response<PMS_MACRODATI>> GetPage(
        [FromBody] Request<CLA_CLA_PMS> req
    ) => base.GetPage(req, query =>
    {
        var hasAssociati = req.Like.TryGetValueAs<bool>("Associati", out var associati);
        
        if (req.Item != null && hasAssociati)
        {
            if (associati)
                query = query.Where(x => x.cla.Where(
                    c => c.CodiceCLA == req.Item.CodiceCLA &&
                         c.COD_ABI == req.Item.COD_ABI &&
                         c.Versione == req.Item.Versione &&
                         c.CodiceNodo == req.Item.CodiceNodo
                ).Any());
            else
                query = query.Where(x => !x.cla.Where(
                    c => c.CodiceCLA == req.Item.CodiceCLA &&
                         c.COD_ABI == req.Item.COD_ABI &&
                         c.Versione == req.Item.Versione &&
                         c.CodiceNodo == req.Item.CodiceNodo
                ).Any());
        }

        return query;
    });
}
```

---

## Backend: Filtered Join (Associations)

**File**: `allitude-pms-service/server/Api/Cla/GetPage.cs`

```cs
public partial class Cla : EntityController<CLA_CLA_PMS>
{
    [HttpGet("GetAssociated")]
    public Task<Response<CLA_CLA_PMS>> GetAssociated(
        [FromBody] Request<CLA_CLA_PMS> req
    ) => base.GetPage(req, query =>
    {
        return query.Where(x => x.Macrodati.Any());
    });

    [HttpGet("GetUnassociated")]
    public Task<Response<CLA_CLA_PMS>> GetUnassociated(
        [FromBody] Request<CLA_CLA_PMS> req
    ) => base.GetPage(req, query =>
    {
        return query.Where(x => !x.Macrodati.Any());
    });
}
```

---

## Frontend: Generated TypeScript Client

**File**: `allitude-pms-fe/src/client/client.ts` (auto-generated)

```typescript
export interface PMS_DORA {
  Id?: string;
  Name?: string;
  Description?: string;
  CreatedAt?: string;
}

export interface PMSDORARequest {
  Like?: Record<string, any>;
  Expressions?: Record<string, any>;
  Page?: number;
  PageSize?: number;
  Item?: PMS_DORA;
  Items?: PMS_DORA[] | null;
}

export interface PMSDORAResponse {
  items?: PMS_DORA[] | null;
  item?: PMS_DORA;
  totalPages?: number;
  totalItems?: number;
  canRead?: boolean;
  canWrite?: boolean;
}

export class Api {
  dora = {
    getPage: (query: {...}) =>
      this.request<PMSDORAResponse>({
        path: `/api/Dora/GetPage`,
        method: 'GET',
        query: query,
      }),
    create: (data: PMS_DORA[]) =>
      this.request<PMSDORAResponse>({
        path: `/api/Dora/Create`,
        method: 'POST',
        body: data,
      }),
    update: (data: PMSDORARequest) =>
      this.request<PMSDORAResponse>({
        path: `/api/Dora/Update`,
        method: 'PATCH',
        body: data,
      }),
  };
}
```

---

## Frontend: List Page with Filter + Pagination

**File**: `allitude-pms-fe/src/pages/Macrodati/index.tsx`

```typescript
function MacroDatiPage() {
  const api = useQuery(client.macroDati.getPage, {
    OrderBy: "CreatedAt desc",
    Like: { Stato: STATES.PUBBLICATO },
  });

  const handleDelete = async (item: PMS_MACRODATI) => {
    await client.macroDati.delete({ Items: [item] });
    await api.refetch();
  };

  return (
    <>
      <ContentHeader
        title="Macrodati"
        totalItems={api?.state?.totalItems ? `${api?.state?.totalItems} Elementi` : "Caricamento.."}
        disabled={!api?.state?.canWrite}
        buttonLabel="Crea Nuovo"
        onButtonClick={() => navigate(`detail/${EMPTY_GUID}`)}
        isLoading={api.isLoading}
      />

      <Filters
        onSubmit={(values) => api.query({ Like: snev(values) }, 1)}
        onReset={() => api.reset()}
        initialValues={{Nome: '', Descrizione: '', Stato: "Pubblicato"}}
        template={(nodes) => (
          <Flex direction="column">
            {nodes.Nome({ icon: 'user' })}
            {nodes.Descrizione({ icon: 'list' })}
            {nodes.Stato({ icon: 'sort' })}
          </Flex>
        )}
      />

      <SemanticTableGrid
        elements={api?.state?.items}
        columns={[
          { name: 'Nome', selector: 'Nome' },
          { name: 'Descrizione', selector: 'Descrizione', formatter: longDescriptionFormatter },
          { name: 'Data Pubblicazione', selector: 'DataPubblicazione', formatter: dateFormatter },
          {
            name: 'Azioni',
            formatter: (row) => (
              <>
                {api.state?.canWrite && (
                  <IconButton icon="trash" onClick={() => handleDelete(row)} />
                )}
              </>
            )
          }
        ]}
        page={api.page}
        onPageChange={(p) => api.query({}, p)}
        isLoading={api.isLoading}
      />
    </>
  );
}
```

---

## Frontend: Detail Page with Form

**File**: `allitude-pms-fe/src/pages/Macrodati/detail.tsx`

```typescript
function MacroDatiDetail() {
  const { id, versione } = useParams<{ id: string; versione: string }>();

  const api = useQuery(client.macroDati.getItem, {
    Like: { Id: id, Versione: parseInt(versione) }
  });

  const handleSave = async (values: PMS_MACRODATI) => {
    try {
      const response = await client.macroDati.update({ Item: values });
      
      if (response.canWrite) {
        showSuccess('Salvato con successo');
        api.refetch();
      } else {
        showError('Non hai permessi di scrittura');
      }
    } catch (error) {
      showError('Errore: ' + error.message);
    }
  };

  return (
    <>
      <ContentHeader title="Dettagli Macrodato" isLoading={api.isLoading} />

      {api.isLoading && <Skeleton />}

      {api.state?.item && (
        <Jormik
          data={api.state.item}
          setData={api.setter}
          loading={api.isLoading}
          validationSchema={{
            Nome: Yup.string().required('Nome obbligatorio'),
            Descrizione: Yup.string().required('Descrizione obbligatoria'),
          }}
          template={{
            section: (nodes) => (
              <>
                {nodes.Nome({ icon: 'edit' })}
                {nodes.Descrizione({ area: true })}
                {nodes.Stato({ icon: 'sort' })}
              </>
            ),
          }}
        >
          {({ values }) => (
            <Flex gap="8px">
              {api.state?.canWrite && (
                <button onClick={() => handleSave(values)}>Salva</button>
              )}
              <button onClick={() => navigate(-1)}>Indietro</button>
            </Flex>
          )}
        </Jormik>
      )}
    </>
  );
}
```

---

## References

- **Backend Types**: `allitude-pms-service/server/client.ts`
- **Frontend Usage**: `allitude-pms-fe/src/pages/`
- **Generated Client**: `allitude-pms-fe/src/client/client.ts` (auto-generated)
- **Hooks**: `allitude-pms-fe/src/utils/hooks/useQuery.tsx`
- **Components**: `allitude-pms-fe/src/components/`
- **Joins Guide**: `docs/09-joins-and-relationships.md`
