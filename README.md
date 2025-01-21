Проект на C#<br>
======
Состоит из:<br>
------
Консольное приложение Сервер<br>
Консольное приложение Клиент<br>
Сервер передаёт справочную информацию клиенту через MQTT<br>
Для получения информации клиенту необходимо пройти авторизацию, авторизация проходит на стороне сервера через LDAP<br>
Ввод команд на клиенте через обычную консоль<br>
Ввод команд на сервере через обычную консоль или подключившись через telnet с клиента(через bash)<br>


Команды клиента:<br>
------
Подключение к консоли: docker attach mqtt-client<br>
Подключение к bash:  docker exec -it mqtt-client /bin/bash<br>

**В консоли:**<br>
Подключение к MQTT серверу: connect<br>
Остановка/Продолжение вывода сообщений: p / r<br>
<em>Сообщения, полученные пока вывод остановлен сохраняются в очередь</em><br>
Остановка клиента: exit<br>


Команды сервера:<br>
-------
Подключение к консоли: docker attach mqtt-server<br>
Подключение к bash консоли:  docker exec -it mqtt-server /bin/bash<br>
Подключение через telnet: telnet mqtt-server 23<br>

**В консоли:**<br>
Остановка/Продолжение публикации сообщений: p / r<br>
Остановка сервера: exit<br>

**Telnet команды:**<br>
Остановка/Продолжение публикации сообщений: p / r<br>
Прекращение telnet подключения: exit<br>
