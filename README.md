# Практическая работа 6

Архипов, 3993. Вариант 2 доска объявлени

## Запуск

Из `6/Practice6`:

```bash
dotnet run --launch-profile http
```

(или just, типа меньше буков)

Swagger: http://localhost:5056/swagger

На пк надо JWT-ключв отдельный User Secrets ID, у меня это `practice6-arkhipov-3993-variant02`. На другом можно перед первым запуском свой случайный ключ:

```bash
dotnet user-secrets set "Jwt:Secret" "<случайная строка не короче 32 символов>"
```

В моем macOS файл находится в `~/.microsoft/usersecrets/practice6-arkhipov-3993-variant02/secrets.json`

Учётные записи Moderator и Admin создаются при запуске, если заданы соответствующие секреты. Например:

```bash
dotnet user-secrets set "Seed:Moderator:Email" "moderator@example.com"
dotnet user-secrets set "Seed:Moderator:Password" "<свой пароль>"
dotnet user-secrets set "Seed:Admin:Email" "admin@example.com"
dotnet user-secrets set "Seed:Admin:Password" "<свой пароль>"
```

## Проверка через Swagger

1. `GET /api/config` → Development, лимит `1048576`.
2. `POST /api/auth/register`: `{"email":"first@example.com","password":"Password123!"}`. Скопировать `accessToken` в Authorize.
3. `POST /api/ads`: `{"title":"Книга","text":"Учебник"}`. Скопировать `id` объявления.
4. `POST /api/ads/{adId}/files`: выбрать JPG или PNG в поле `file` → 201. Скопировать `id` файла из ответа.
5. `GET /api/ads/{adId}/files` → список. `GET /api/files/{id}` → скачать файл, 200.
6. Зарегистрировать второго пользователя и подставить его токен. Скачать первый файл → 403. Убрать токен → 401.
7. Загрузить PDF, файл больше 1 МБ или PNG с расширением `.jpg` → 400.
8. Модератором вызвать `PATCH /api/ads/{id}/hide`. Автором загрузить новое фото → 403; скачать старое → 200.

Файлы: `6/Practice6/storage/uploads/год/месяц/день/`. Папка появляется после первой успешной загрузки. Имя на диске — GUID; исходное имя и путь записаны в таблице `StoredFiles` базы `6/Practice6/practice6.db`.
