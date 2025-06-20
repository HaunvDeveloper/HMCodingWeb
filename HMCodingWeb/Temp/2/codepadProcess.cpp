#include <iostream>
#include <vector>
#include <cmath>
using namespace std;

bool isPrime(int x) {
    if (x < 2) return false;
    int sqrtX = sqrt(x);
    for (int i = 2; i <= sqrtX; ++i) {
        if (x % i == 0) return false;
    }
    return true;
}

int main() {
    int n;
    cin >> n;
    vector<int> a(n);

    int maxPrime = -1;

    for (int i = 0; i < n; ++i) {
        cin >> a[i];
        if (isPrime(a[i])) {
            maxPrime = max(maxPrime, a[i]);
        }
    }

    cout << maxPrime << endl;
    return 0;
}
