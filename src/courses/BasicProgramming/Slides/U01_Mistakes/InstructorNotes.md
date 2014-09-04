﻿
# Аудиторные практики

## Общие рекомендации

В начале пары стоит разобрать хотя бы одну, самую сложную задчу, заданную на дом.

Задачи для продвинутых имеет смысл давать во время аудиторной практики, 
если возникает необходимость нейтрализовать (затормозить) некоторых сильных студентов или тех, 
кто по каким-то причинам знаком с предлагаемыми задачами (такими часто бывают второгодники).

## Простые алгоритмы

1. Обмен двух переменных. 

	Цель: понять как работают переменные, что a = b; b = a не работает.

2. Задается натуральное трехзначное число (гарантируется, что трехзначное).
	Развернуть его, т.е. получить трехзначное число, записанное теми же цифрами в обратном порядке.

	Цель: познакомиться с делимостью, обсудить, что просто напечатать цифры в нужном порядке 
	это совсем не то же самое, что сформировать новое число.

3. Задано время Н часов (ровно). Вычислить угол в градусах между часовой и минутной стрелками.
	Например, 5 часов -> 150 градусов, 20 часов -> 120 градусов. Не использовать циклы.

	Цель: продемонстрировать основные этапы разработки алгоритмов: 
	построить модель задачи, написать формулу для 0<=H<=6, понять, 
	что для Н до 12 можно обойтись без if за счет модуля, обобщить для Н до 24 за счет остатка от деления на 12.


4. __ДЗ.__ Вариант задачи 1.3, где задано время в часах и минутах.

5. Найти количество чисел меньших N, которые имеют простые делители X или Y.

	Цель: еще одна задача на делимость, неявная помощь в решении следующих двух задач.

6. __ДЗ.__ Количество високосных лет на отрезке [a, b].

7. __ДЗ.__ [Project Euler 1](http://projecteuler.net/problem=1)
	Find the sum of all the multiples of 3 or 5 below 1000.

8. Нужен параллелепипед из бумаги со сторонами a, b, c.
	Найти координаты всех вершин какой-нибудь развертки.

9. __ДОП.__ Мальчик Вася хочет сделать параллелипипед из бумаги со сторонами a, b, c. 
	Сделайте ему такую (связную) развертку параллелепипида, чтобы минимизировать количество 
	используемого клея (то есть минимизировать суммарную длину всех ребер, которые придется склеить).