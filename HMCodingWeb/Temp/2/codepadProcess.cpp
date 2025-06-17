#include <bits/stdc++.h>
using namespace std;

double s(double x, double kq) {

    if(x < -1)
        kq = pow(x, 1.0 / 3.0);
    else if (x >= -1)
        kq = acos(x);
    else
        kq = sin(x) + pow(M_E, sqrt(x));
    return kq;
}

int main() {
    double x;
    cin>>x;
    double kq;
    cout << fixed << setprecision(2) << s(x, kq);
    return 0;
}