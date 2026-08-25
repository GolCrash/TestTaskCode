import { useEffect, useState } from 'react'
import axios from 'axios'
import { AgGridReact } from 'ag-grid-react'
import type { ColDef } from 'ag-grid-community'

import 'ag-grid-community/styles/ag-grid.css'
import 'ag-grid-community/styles/ag-theme-alpine.css'

interface TimeEntry {
  id: string
  employeeId: string
  employeeName: string
  projectId: string
  projectCode: string
  hours: number
  amount: number
  date: string
  comment: string
  isOvertime: boolean
  rate: number
  version: number
}

interface TimeEntryListResponse {
  items: TimeEntry[]
  page: number
  pageSize: number
  totalCount: number
  totalPage: number
}

interface Employee {
  id: string
  fullName: string
  department: string
}

interface Project {
  id: string
  projectCode: string
  name: string
}

interface ProjectReportRow {
  projectId: string
  projectCode: string
  projectName: string
  hours: number
  amount: number
  budget: number
  percent: number
  overspent: boolean
  risk: boolean
}

interface ProjectReportResponse {
  items: ProjectReportRow[]
  totalHours: number
  totalAmount: number
}

function App() {
  const [entries, setEntries] = useState<TimeEntry[]>([])
  const [employees, setEmployees] = useState<Employee[]>([])
  const [projects, setProjects] = useState<Project[]>([])

  const [year, setYear] = useState(2026)
  const [month, setMonth] = useState(3)
  const [employeeId, setEmployeeId] = useState('')
  const [projectId, setProjectId] = useState('')

  const [page, setPage] = useState(1)
  const [pageSize] = useState(20)
  const [totalPage, setTotalPage] = useState(1)

  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)

  const [formEmployeeId, setFormEmployeeId] = useState('')
  const [formProjectId, setFormProjectId] = useState('')
  const [formHours, setFormHours] = useState('')
  const [formDate, setFormDate] = useState('')
  const [formComment, setFormComment] = useState('')
  const [formVersion, setFormVersion] = useState(1)

  const [report, setReport] = useState<ProjectReportRow[]>([])
const [reportYear, setReportYear] = useState(2026)
const [reportMonth, setReportMonth] = useState(3)
const [reportLoading, setReportLoading] = useState(false)
const [reportError, setReportError] = useState('')
const [reportTotalHours, setReportTotalHours] = useState(0)
const [reportTotalAmount, setReportTotalAmount] = useState(0)

    const columns: ColDef<TimeEntry>[] = [
    { field: 'employeeName', headerName: 'Сотрудник' },
    { field: 'projectCode', headerName: 'Проект' },
    { field: 'hours', headerName: 'Часы' },
    { field: 'amount', headerName: 'Сумма' },
    { field: 'date', headerName: 'Дата' },
    { field: 'comment', headerName: 'Комментарий' },
    { field: 'isOvertime', headerName: 'Сверхурочные' },
    {
    headerName: 'Действия',
    cellRenderer: (params: { data: TimeEntry }) => (
    <div>
      <button onClick={() => openEditForm(params.data)}>
        Изменить
      </button>

      <button onClick={() => deleteEntry(params.data)}>
        Удалить
      </button>
    </div>
  ),
},
  ]

  useEffect(() => {
    loadDirectories()
  }, [])

  useEffect(() => {
    loadEntries()
  }, [page])

  async function loadDirectories() {
    try {
      const [employeesResponse, projectsResponse] = await Promise.all([
        axios.get<Employee[]>('/api/employees'),
        axios.get<Project[]>('/api/projects'),
      ])

      setEmployees(employeesResponse.data)
      setProjects(projectsResponse.data)
    } catch (error) {
      console.error(error)
      setError('Не удалось загрузить справочники.')
    }
  }

  async function loadReport() {
  try {
    setReportLoading(true)
    setReportError('')

    const response = await axios.get<ProjectReportResponse>(
      '/api/reports/projects',
      {
        params: {
          year: reportYear,
          month: reportMonth,
        },
      }
    )

    setReport(response.data.items)
    setReportTotalHours(response.data.totalHours)
    setReportTotalAmount(response.data.totalAmount)
  } catch (error) {
    console.error(error)
    setReportError('Не удалось загрузить отчёт.')
  } finally {
    setReportLoading(false)
  }
}

  async function deleteEntry(entry: TimeEntry) {
  try {
    setError('')

    await axios.delete(`/api/time-entries/${entry.id}`, {
      params: {
        version: entry.version,
      },
    })

    await loadEntries()
  } catch (error) {
    console.error(error)

    if (axios.isAxiosError(error)) {
      setError(
        error.response?.data?.message ||
        JSON.stringify(error.response?.data) ||
        'Не удалось удалить запись.'
      )
    } else {
      setError('Не удалось удалить запись.')
    }
  }
}

  async function loadEntries() {
    try {
      setLoading(true)
      setError('')

      const response = await axios.get<TimeEntryListResponse>(
        '/api/time-entries',
        {
          params: {
            year,
            month,
            employeeId: employeeId || undefined,
            projectId: projectId || undefined,
            page,
            pageSize,
          },
        }
      )

      setEntries(response.data.items)
      setTotalPage(response.data.totalPage)
    } catch (error) {
      console.error(error)
      setError('Не удалось загрузить записи.')
    } finally {
      setLoading(false)
    }
  }

  function applyFilters() {
  setPage(1)

  if (page === 1) {
    loadEntries()
  }
}

  function previousPage() {
    if (page > 1) {
      setPage(page - 1)
    }
  }

  function nextPage() {
    if (page < totalPage) {
      setPage(page + 1)
    }
  }

  function openCreateForm() {
    setEditingId(null)
    setFormEmployeeId('')
    setFormProjectId('')
    setFormHours('')
    setFormDate('')
    setFormComment('')
    setFormVersion(1)
    setShowForm(true)
}

  function openEditForm(entry: TimeEntry) {
    setEditingId(entry.id)
    setFormEmployeeId(entry.employeeId)
    setFormProjectId(entry.projectId)
    setFormHours(String(entry.hours))
    setFormDate(entry.date.substring(0, 10))
    setFormComment(entry.comment)
    setFormVersion(entry.version)
    setShowForm(true)
}

async function saveEntry() {
  try {
    setError('')

    const hours = Number(formHours)

    if (!formEmployeeId) {
      setError('Выберите сотрудника.')
      return
    }

    if (!formProjectId) {
      setError('Выберите проект.')
      return
    }

    if (!formDate) {
      setError('Укажите дату.')
      return
    }

    if (!Number.isFinite(hours) || hours <= 0) {
      setError('Количество часов должно быть больше 0.')
      return
    }

    if (hours % 0.5 !== 0) {
      setError('Количество часов должно быть кратно 0,5.')
      return
    }

    if (hours > 24) {
      setError('Количество часов не должно превышать 24.')
      return
    }

    const request = {
      employeeId: formEmployeeId,
      projectId: formProjectId,
      hours,
      date: formDate,
      comment: formComment,
      version: formVersion,
    }

    if (editingId === null) {
  await axios.post('/api/time-entries', request)
} else {
  await axios.put(`/api/time-entries/${editingId}`, request)
}

    setShowForm(false)
    await loadEntries()
  } catch (error) {
    console.error(error)

    if (axios.isAxiosError(error)) {
      const data = error.response?.data

      if (data?.message) {
        setError(data.message)
      } else if (data?.errors) {
        const messages = Object.values(data.errors)
          .flat()
          .join(' ')

        setError(messages)
      } else {
        setError('Не удалось сохранить запись.')
      }
    } else {
      setError('Не удалось сохранить запись.')
    }
  }
}

const totalHours = entries.reduce(
  (sum, entry) => sum + entry.hours,
  0
)

const totalAmount = entries.reduce(
  (sum, entry) => sum + entry.amount,
  0
)

  return (
    <div>
      {showForm && (
        <div>
          <h2>
            {editingId === null ? 'Новая запись' : 'Редактирование записи'}
          </h2>

          <div>
            <label>
              Сотрудник:
              <select
                value={formEmployeeId}
                onChange={(e) => setFormEmployeeId(e.target.value)}
              >
                <option value="">Выберите сотрудника</option>

                {employees.map((employee) => (
                  <option key={employee.id} value={employee.id}>
                    {employee.fullName}
                  </option>
                ))}
              </select>
            </label>
          </div>

          <div>
            <label>
              Проект:
              <select
                value={formProjectId}
                onChange={(e) => setFormProjectId(e.target.value)}
              >
                <option value="">Выберите проект</option>

                {projects.map((project) => (
                  <option key={project.id} value={project.id}>
                    {project.projectCode} — {project.name}
                  </option>
                ))}
              </select>
            </label>
          </div>

          <div>
            <label>
              Часы:
              <input
                type="number"
                value={formHours}
                onChange={(e) => setFormHours(e.target.value)}
              />
            </label>
          </div>

          <div>
            <label>
              Дата:
              <input
                type="date"
                value={formDate}
                onChange={(e) => setFormDate(e.target.value)}
              />
            </label>
          </div>

          <div>
            <label>
              Комментарий:
              <input
                type="text"
                value={formComment}
                onChange={(e) => setFormComment(e.target.value)}
              />
            </label>
          </div>

          {editingId !== null && (
            <div>
              Версия: {formVersion}
            </div>
          )}

          <button onClick={saveEntry}>
            Сохранить
          </button>

          <button onClick={() => setShowForm(false)}>
            Отмена
          </button>
        </div>
      )}

      <h1>Табель</h1>

      <button onClick={openCreateForm}>
        Добавить запись
      </button>

      <div>
        <label>
          Год:
          <input
            type="number"
            value={year}
            onChange={(e) => setYear(Number(e.target.value))}
          />
        </label>

        <label>
          Месяц:
          <input
            type="number"
            min="1"
            max="12"
            value={month}
            onChange={(e) => setMonth(Number(e.target.value))}
          />
        </label>

        <label>
          Сотрудник:
          <select
            value={employeeId}
            onChange={(e) => setEmployeeId(e.target.value)}
          >
            <option value="">Все сотрудники</option>

            {employees.map((employee) => (
              <option key={employee.id} value={employee.id}>
                {employee.fullName}
              </option>
            ))}
          </select>
        </label>

        <label>
          Проект:
          <select
            value={projectId}
            onChange={(e) => setProjectId(e.target.value)}
          >
            <option value="">Все проекты</option>

            {projects.map((project) => (
              <option key={project.id} value={project.id}>
                {project.projectCode} — {project.name}
              </option>
            ))}
          </select>
        </label>

        <button onClick={applyFilters}>
          Применить
        </button>
      </div>

      {error && <div>{error}</div>}

      {loading ? (
        <div>Загрузка...</div>
      ) : (
        <>
          <div
            className="ag-theme-alpine"
            style={{ height: 500, width: '100%' }}
          >
            <AgGridReact
              rowData={entries}
              columnDefs={columns}
            />
          </div>

          <hr />

<h1>Отчёт по проектам</h1>

<div>
  <label>
    Год:
    <input
      type="number"
      value={reportYear}
      onChange={(e) => setReportYear(Number(e.target.value))}
    />
  </label>

  <label>
    Месяц:
    <input
      type="number"
      min="1"
      max="12"
      value={reportMonth}
      onChange={(e) => setReportMonth(Number(e.target.value))}
    />
  </label>

  <button onClick={loadReport}>
    Сформировать отчёт
  </button>
</div>

{reportError && <div>{reportError}</div>}

{reportLoading ? (
  <div>Загрузка отчёта...</div>
) : (
  <table>
    <thead>
      <tr>
        <th>Проект</th>
        <th>Часы</th>
        <th>Стоимость</th>
        <th>Бюджет</th>
        <th>Освоено</th>
      </tr>
    </thead>

    <tbody>
      {report.map((row) => (
        <tr key={row.projectId}>
          <td>
            {row.projectCode} — {row.projectName}
          </td>

          <td>
            {row.hours}
          </td>

          <td>
            {row.amount.toFixed(2)} ₽
          </td>

          <td>
            {row.budget.toFixed(2)} ₽
          </td>

          <td>
            {row.percent.toFixed(2)}%

            {row.overspent && (
              <span> — ПЕРЕРАСХОД</span>
            )}

            {!row.overspent && row.risk && (
              <span> — РИСК</span>
            )}
          </td>
        </tr>
      ))}
    </tbody>

    <tfoot>
      <tr>
        <td>Итого</td>
        <td>{reportTotalHours}</td>
        <td>{reportTotalAmount.toFixed(2)} ₽</td>
        <td></td>
        <td></td>
      </tr>
    </tfoot>
  </table>
)}
<div>
  <div>
    Итого часов: {totalHours}
  </div>

  <div>
    Итого стоимость: {totalAmount.toFixed(2)} ₽
  </div>
</div>
          <div>
            <button
              onClick={previousPage}
              disabled={page <= 1}
            >
              Назад
            </button>

            <span>
              Страница {page} из {totalPage}
            </span>

            <button
              onClick={nextPage}
              disabled={page >= totalPage}
            >
              Вперёд
            </button>
          </div>
        </>
      )}
    </div>
  )
}

export default App