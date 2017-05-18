# electric-meter

Программа опроса электросчетчиков по порту

-- Меркурий 230тых моделей , Меркурий 206

Функции 
- снятие показаний по месяцам за год
- снятия показаний по дням (за прошлый день и на начало текущего дня)
- корректировка времени (по часам компьютера)
- снятие мгновенных показаний тока, напряжения , коэф мощности, частоты
- ведение базы. Если данные отсутствуют за конкретный период
- контроль лимитов (по токам , мощностям и напряжениям)

Для работы требуется COM порт и Mysql с таблицей вида (имя таблицы задается в Meter_conf.xml)

    

 Field               | Type                 | Null | Key | Default | Extra          
---------------------|----------------------|------|-----|---------|----------------
 index               | int(10) unsigned     | NO   | PRI | NULL    | auto_increment 
 addr                | smallint(5) unsigned | NO   |     | NULL    |                
 energy_active_in    | int(11)              | YES  |     | NULL    |                
 energy_reactive_in  | int(11)              | YES  |     | NULL    |                
 energy_reactive_out | int(11)              | YES  |     | NULL    |                
 period              | int(10)              | NO   |     | -1      |                
 oleDT               | double               | NO   |     | NULL    |                
 tariff              | smallint(5) unsigned | YES  |     | NULL    |                
 id                  | int(11)              | NO   |     | -1      |                
 month               | smallint(6)          | NO   |     | -1      |                

Для контроля лимитов - таблица "dumpmeters" вида 

| Field | Type                 | Null | Key | Default | Extra          |
--------|----------------------|------|-----|---------|----------------|
| index | int(10) unsigned     | NO   | PRI | NULL    | auto_increment |
| addr  | smallint(5) unsigned | NO   |     | NULL    |                |
| id    | int(11)              | NO   |     | -1      |                |
| dump  | text                 | NO   |     | NULL    |                |
