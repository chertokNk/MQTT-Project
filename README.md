Проект на C#<br>
======
Состоит из:<br>
------
Консольное приложение MQTT.Server<br>
Консольное приложение MQTT.Client<br>
Сервер передаёт справочную информацию клиенту через MQTT<br>
Для получения информации клиенту необходимо пройти авторизацию, авторизация проходит на стороне сервера через LDAP<br>
Ввод команд на клиенте через обычную консоль<br>
Ввод команд на сервере через обычную консоль или подключившись через telnet<br>
Telnet доступен только для admin

**Так же настроен phpLDAPadmin**, в основном для простой проверки LDAP данных<br>
Доступ через: http://localhost:8090<br>
Login: cn=admin,dc=chnk,dc=org<br>
Pass: 12345<br>

Команды клиента<br>
------
Подключение к консоли: docker attach mqtt-client<br>
Подключение к bash контейнера:  docker exec -it mqtt-client /bin/bash<br>

**В консоли:**<br>
Подключение к MQTT серверу: connect<br>
Отключение от MQTT сервера: disconnect<br>
Остановка/Продолжение вывода сообщений: p / r<br>
<em>Сообщения, полученные пока вывод остановлен сохраняются в очередь</em><br>
Остановка клиента: exit<br>


Команды сервера<br>
-------
Подключение к консоли: docker attach mqtt-server<br>
Подключение к bash контейнера:  docker exec -it mqtt-server /bin/bash<br>
Подключение к серверу через telnet из контейнера: telnet mqtt-server 23<br>
Подключение к серверу через telnet извне: telnet localhost 23 (поддерживает как bash так и powershell)<br>

**В консоли:**<br>
Остановка/Продолжение публикации сообщений: p / r<br>
Получить список всех клиентов: getall<br>
Отключить всех клиентов: kickall<br>
Отключить конкретного клиента: kick<br>
Остановка сервера: shutdown<br>

**Telnet команды:**<br>
Остановка/Продолжение публикации сообщений: p / r<br>
Получить список всех клиентов: getall<br>
Отключить всех клиентов: kickall<br>
Отключить конкретного клиента: kick<br>
Остановка сервера: shutdown<br>
Прекращение telnet подключения: exit<br>

Данные пользователей
======
admin<br>
user1<br>
user2<br>
user3<br>

**Пароль**<br>
12345 (для всех)<br>
