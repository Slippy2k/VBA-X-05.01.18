#pragma once
#include <string>
#include <vector>

void split(const std::string &s, char delim, std::vector<std::string> *elems);

bool stringWhitespace(const std::string &s);

template<class T, class C>
int firstIndexOf(T &s, C c)
{
	for (int i = 0; i < s.size(); ++i)
	{
		if (s[i] == c) return i;
	}
	return -1;
}

template<class T, class C>
int lastIndexOf(T &s, C c)
{
	for (int i = s.size() - 1; i >= 0; i--)
	{
		if (s[i] == c) return i;
	}
	return -1;
}

std::string &strreplace(std::string &input, char oldChar, char newChar);

std::vector<std::string> &strSplitLines(std::string &input, std::vector<std::string> &v);

void replaceAll(std::string& str, const std::string& from, const std::string& to);

void StrToUpper(std::string &input);