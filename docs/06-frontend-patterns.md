# Frontend Patterns

Real-world React patterns using generated TypeScript client in `allitude-pms-fe`.

## useQuery Hook

Dead simple API wrapper inspired by TanStack Query, but lighter.

### Basic Usage

```typescript
import { useQuery } from 'utils/hooks/useQuery';
import client from 'services/client/request';

function MacroDatiPage() {
  const api = useQuery(client.macroDati.getPage, {
    OrderBy: 'CreatedAt desc',
    Like: { Stato: STATES.PUBBLICATO },
  });

  return (
    <div>
      {api.isLoading && <Skeleton />}
      <table>
        {api.state?.items?.map(item => (
          <tr key={item.Id}>{item.Nome}</tr>
        ))}
      </table>
      
      <Pagination
        current={api.page}
        total={api.state?.totalPages || 0}
        onChange={(p) => api.setPage(p)}
      />
    </div>
  );
}
```

### Reactive Filtering

```typescript
const handleFilter = (values: FilterFormValues) => {
  api.query({ 
    Like: {
      Nome: values.Nome,
      Descrizione: values.Descrizione,
      Stato: values.Stato
    }
  }, 1);  // Jump to page 1
};

api.reset();  // Clear all filters
```

### Query Lifecycle

```typescript
const api = useQuery(promise, initialParams, onSuccess?, onLoading?);

// State
api.state          // Current response (Response<T>)
api.params         // Current request params
api.page           // Current page (1-indexed)
api.isLoading      // Loading flag

// Methods
api.query(updates, page?)     // Merge params & refetch
api.refetch()                 // Re-execute current query
api.reset()                   // Clear to initial params
api.setter(newState)          // Manual state override

// Permissions
api.canRead        // Shortcut to api.state?.canRead
api.canWrite       // Shortcut to api.state?.canWrite
```

---

## Data Table Pattern

Combine `useQuery` with SemanticTableGrid for paginated, sortable table:

```typescript
import SemanticTableGrid from 'semantic-table-grid';

function MacroDatiTable() {
  const api = useQuery(client.macroDati.getPage, {
    OrderBy: 'CreatedAt desc',
    Like: { Stato: STATES.PUBBLICATO },
  });

  return (
    <SemanticTableGrid
      elements={api?.state?.items}
      columns={[
        {
          name: 'Nome',
          selector: 'Nome',
          sortable: true,
          onSort: (col, dir) => {
            const orderBy = `${col} ${dir === 'desc' ? 'desc' : ''}`;
            api.query({ OrderBy: orderBy }, 1);
          }
        },
        {
          name: 'Descrizione',
          selector: 'Descrizione',
          formatter: longDescriptionFormatter,
        },
        {
          name: 'Data Pubblicazione',
          selector: 'DataPubblicazione',
          formatter: dateFormatter,
        },
        {
          name: 'Azioni',
          formatter: (row) => (
            <IconButton
              onClick={() => navigate(`detail/${row.Id}/${row.Versione}`)}
              icon="edit"
            />
          )
        }
      ]}
      pageSize={api.state?.totalPages}
      page={api.page}
      onPageChange={(p) => api.query({}, p)}
      isLoading={api.isLoading}
    />
  );
}
```

---

## Filter Form Pattern

Formik-based filters that reactively query:

```typescript
export function Filters<T>({onSubmit, onReset, initialValues, schema, template}: FilterProps<T>) {
  return (
    <Formik
      initialValues={initialValues}
      onSubmit={onSubmit}
      enableReinitialize
    >
      {({ values, resetForm }) => (
        <Form>
          {template(createNodes(values, schema))}
          
          <button type="submit">Cerca</button>
          <button 
            type="button"
            onClick={() => {
              resetForm();
              onReset();
            }}
          >
            Ripristina
          </button>
        </Form>
      )}
    </Formik>
  );
}

// Usage in page
const api = useQuery(client.macroDati.getPage, {...});

return (
  <Filters
    onSubmit={(values) => api.query({ Like: snev(values) }, 1)}
    onReset={() => api.reset()}
    initialValues={{...}}
    template={(nodes) => (...)}
  />
);
```

---

## Detail Page Pattern

Fetch single item, edit in form, save back:

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
            Stato: Yup.string().required('Stato obbligatorio'),
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
              {api.state.canWrite && (
                <button
                  onClick={() => handleSave(values)}
                  className="button--highlight"
                >
                  Salva
                </button>
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

## Create New Item Pattern

Pre-fill form with defaults, validate, create on backend:

```typescript
function CreateMacrodato() {
  const navigate = useNavigate();

  const handleCreate = async (formData: PMS_MACRODATI) => {
    try {
      const newItem: PMS_MACRODATI = {
        ...formData,
        Id: uuidv4(),
        CreatedAt: new Date().toISOString(),
        Versione: 1,
      };

      const response = await client.macroDati.create([newItem]);

      if (response.canWrite) {
        showSuccess('Macrodato creato con successo');
        navigate(`detail/${newItem.Id}/1`);
      } else {
        showError('Non hai permessi di scrittura');
      }
    } catch (error) {
      showError('Errore creazione: ' + error.message);
    }
  };

  return (
    <>
      <ContentHeader title="Crea Nuovo Macrodato" />

      <Jormik
        data={{
          Nome: '',
          Descrizione: '',
          Stato: STATES.BOZZA,
        }}
        setData={() => {}}
        validationSchema={{
          Nome: Yup.string().required('Nome obbligatorio'),
          Descrizione: Yup.string().required('Descrizione obbligatoria'),
        }}
        template={(nodes) => (
          <>
            {nodes.Nome({ icon: 'edit' })}
            {nodes.Descrizione({ area: true })}
            {nodes.Stato({ icon: 'sort' })}
          </>
        )}
      >
        {({ values }) => (
          <Flex gap="8px">
            <button
              onClick={() => handleCreate(values)}
              className="button--highlight"
            >
              Crea
            </button>
          </Flex>
        )}
      </Jormik>
    </>
  );
}
```

---

## Delete with Confirmation

Modal confirmation before hard/soft delete:

```typescript
function MacroDatiTable() {
  const api = useQuery(client.macroDati.getPage, {...});
  const modalContext = useReducer(ModalContext);

  const handleDelete = async () => {
    if (modalContext.values.selectedItem) {
      modalContext.set({ isLoading: true });

      try {
        await client.macroDati.delete({
          Items: [modalContext.values.selectedItem]
        });

        showSuccess('Eliminato con successo');
        modalContext.set({ isOpen: false });
        await api.refetch();
      } catch (error) {
        showError('Errore: ' + error.message);
      } finally {
        modalContext.set({ isLoading: false });
      }
    }
  };

  return (
    <>
      <SemanticTableGrid
        elements={api.state?.items}
        columns={[
          // ...
          {
            name: 'Azioni',
            formatter: (row) => (
              <IconButton
                onClick={() => {
                  modalContext.set({
                    isOpen: true,
                    selectedItem: row,
                  });
                }}
                icon="trash"
                disabled={!api.state?.canWrite}
              />
            )
          }
        ]}
      />

      {modalContext.values.isOpen && (
        <Modal header="Elimina Macrodato?">
          <p>Sei sicuro?</p>
          <Flex gap="8px">
            <button onClick={handleDelete}>Elimina</button>
            <button onClick={() => modalContext.set({ isOpen: false })}>
              Annulla
            </button>
          </Flex>
        </Modal>
      )}
    </>
  );
}
```

---

## Jormik (Type-Driven Forms)

Declarative form generation from types:

```typescript
<Jormik
  data={api.state?.item}
  setData={api.setter}
  loading={api.isLoading}
  validationSchema={{
    Nome: Yup.string().required('Nome obbligatorio'),
    Descrizione: Yup.string().required('Descrizione obbligatoria'),
  }}
  components={JormikComponents}
  template={{
    section: (nodes) => (
      <Flex direction="column" gap="16px">
        {nodes.Nome({ icon: 'edit' })}
        {nodes.Descrizione({ area: true })}
      </Flex>
    ),
  }}
>
  {({ values, isSubmitting }) => (
    <Flex gap="8px">
      {api.state?.canWrite && (
        <button onClick={() => client.macroDati.update({ Item: values })}>
          Salva
        </button>
      )}
    </Flex>
  )}
</Jormik>
```

---

## Permission-Based UI

Always respect `canRead` / `canWrite` flags:

```typescript
function EntityActions({ item, canWrite, onEdit, onDelete }) {
  return (
    <Flex gap="4px">
      {canWrite ? (
        <>
          <IconButton icon="edit" onClick={() => onEdit(item)} />
          <IconButton icon="trash" onClick={() => onDelete(item)} />
        </>
      ) : (
        <IconButton icon="lock" disabled title="Read-only" />
      )}
    </Flex>
  );
}

// In table
{
  name: 'Actions',
  formatter: (row) => (
    <EntityActions
      item={row}
      canWrite={api.state?.canWrite}
      onEdit={handleEdit}
      onDelete={handleDelete}
    />
  )
}
```
