﻿///////////////////////////////////////////////////////////////////////
//
// Тест проверки работы с исключениями
// 
//
///////////////////////////////////////////////////////////////////////

Перем юТест;

////////////////////////////////////////////////////////////////////
// Программный интерфейс

Функция ПолучитьСписокТестов(ЮнитТестирование) Экспорт
	
	юТест = ЮнитТестирование;
	
	ВсеТесты = Новый Массив;
	
	ВсеТесты.Добавить("ТестДолженПроверитьИсключенияИдущиеДругЗаДругом");
	ВсеТесты.Добавить("ТестДолжен_ВызватьФункциюКакПроцедуру_Issue104");
	ВсеТесты.Добавить("ТестДолжен_ПрочитатьЛитералДатыСРазделителями");
	
	Возврат ВсеТесты;
	
КонецФункции

Процедура ТестДолженПроверитьИсключенияИдущиеДругЗаДругом() Экспорт
	ТекстОшибки = "";
	Попытка
		БроситьИсключение(1, 2);
	Исключение
		ТекстОшибки = ОписаниеОшибки();
		Сообщить("внутри исключения (1, 2)");
	КонецПопытки;
	
	юТест.ПроверитьНеравенство("", ТекстОшибки, "Должны были получить текст исключения, но не получили");
	
КонецПроцедуры

Процедура БроситьИсключение(Парам1, Парам2)
	Сообщить("до "+Парам1);
	Попытка
		ВызватьИсключение Парам1;
	Исключение
		ТекстОшибки = ОписаниеОшибки();
		Сообщить("внутри исключения "+Парам1);
	КонецПопытки;

	Сообщить("до "+Парам2);
	ВызватьИсключение Парам2;
КонецПроцедуры

Функция ВызовЛомающийСтек()
    
	Если ЗначениеЗаполнено(0) Тогда
		А = 2;
	КонецЕсли;
	
    Возврат 1;
	
КонецФункции

Функция НекаяФункция()
	ВызовЛомающийСтек();
	Возврат 1;
КонецФункции

Процедура ТестДолжен_ВызватьФункциюКакПроцедуру_Issue104() Экспорт
	
	юТест.ПроверитьРавенство(1, НекаяФункция()); // сбой передачи аргумента в метод.
	
КонецПроцедуры

Процедура ТестДолжен_ПрочитатьЛитералДатыСРазделителями() Экспорт
	
	юТест.ПроверитьРавенство('20140101121727', '2014-01-01 12:17:27');
	юТест.ПроверитьРавенство('20140101121727', '2014 год 01 месяц 01 число 12 часов 17 минут 27 секунд');
	
КонецПроцедуры